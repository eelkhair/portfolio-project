using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Domain.Entities.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Outbox;

public class ProcessOutboxMessageCommand : BaseCommand<bool>, INoTransaction;

public class ProcessOutboxMessage(
    IHandlerContext context,
    IOutboxDbContext outboxDbContext,
    IOutboxMessageProcessor outboxMessageProcessor,
    IActivityFactory activityFactory
) : BaseCommandHandler(context),
    IHandler<ProcessOutboxMessageCommand, bool>
{
    public async Task<bool> HandleAsync(ProcessOutboxMessageCommand request, CancellationToken cancellationToken)
    {
        var messages = await outboxDbContext.OutboxMessages
            .FromSqlInterpolated($@"SELECT TOP 20 * 
                                FROM outbox.Messages WITH (UPDLOCK, READPAST) 
                                WHERE ProcessedAt IS NULL 
                                ORDER BY CreatedAt")
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            Logger.LogTrace("No new outbox messages to process");
            return false;
        }

        // Preprocess trace parents
        var i = 0;
        var parsedParents = new Dictionary<int, ActivityContext>();

        foreach (var message in messages)
        {
            if (ActivityContext.TryParse(message.TraceParent, null, out var parentContext))
            {
                parsedParents[i] = parentContext;
                Activity.Current?.AddTag($"outbox.message{i}.traceId", parentContext.TraceId.ToString());
            }
            i++;
        }

        using var batchActivity = activityFactory.StartActivity(
            "OutboxBatch",
            ActivityKind.Internal,
            parentContext: Activity.Current?.Context ?? default);

        batchActivity?.AddTag("outbox.batch.count", messages.Count);

        Logger.LogInformation("Found {MessageCount} new outbox messages to process", messages.Count);

        i = 0;

        foreach (var message in messages)
        {
            var parentContext = parsedParents.TryGetValue(i, out var ctx)
                ? ctx
                : default;

            using var activity = SetupTracing(message, parentContext, batchActivity);

            try
            {
                Logger.LogInformation(
                    "Processing outbox message ID: {MessageId}, ParentTraceId: {ParentTrace}",
                    message.InternalId,
                    parentContext.TraceId.ToString());

                await outboxMessageProcessor.ProcessAsync(message, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                MetricsService.IncrementOutboxMessagesProcessed();
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                HandleException(message, ex, activity?.Id);
            }

            await Context.SaveChangesAsync(request.UserId, cancellationToken);
            i++;
        }

        batchActivity?.SetStatus(ActivityStatusCode.Ok);
        return true;
    }


    private void HandleException(OutboxMessage message, Exception ex, string? activityId)
    {
        Logger.LogError(ex, "Failed to process outbox message ID: {MessageId}. Updating retry logic",
            message.InternalId);

        message.RetryCount++;
        message.LastError = ex.Message;

        if (message.RetryCount < 3)
            return;

        message.TraceParent = activityId;
        HandleMaxRetriesReached(message);
    }

    private void HandleMaxRetriesReached(OutboxMessage message)
    {
        Logger.LogCritical(
            "Message ID {MessageId} has reached max retries. Sending to DeadLetter and sending alert",
            message.InternalId);

        var deadLetterMessage = new OutboxDeadLetter
        {
            EventType = message.EventType,
            Payload = message.Payload,
            TraceParent = message.TraceParent,
            CreatedAt = DateTime.UtcNow,
            LastError = message.LastError,
            CreatedBy = message.CreatedBy,
            UpdatedBy = message.UpdatedBy,
            UpdatedAt = DateTime.UtcNow,
            OutboxMessageId = message.InternalId
        };

        outboxDbContext.OutboxDeadLetters.Add(deadLetterMessage);
        outboxDbContext.OutboxMessages.Remove(message);
    }


    private Activity? SetupTracing(
        OutboxMessage message,
        ActivityContext parentContext,
        Activity? batchActivity)
    {
        try
        {
            var hostActivity = Activity.Current;
            // Create consumer span
            var activity = activityFactory.StartActivity(
                "ProcessOutboxMessage",
                ActivityKind.Consumer,
                parentContext);

            if (activity == null)
                return null;

            // Main metadata
            activity.AddTag("outbox.message.id", message.InternalId);
            activity.AddTag("outbox.message.eventType", message.EventType);
            activity.AddTag("outbox.message.payloadSize", message.Payload?.Length ?? 0);
            activity.AddTag("processor.traceId", hostActivity?.TraceId.ToString() ?? activity.TraceId.ToString());
            activity.AddTag("outbox.parent.traceId", parentContext.TraceId.ToString());
            activity.AddTag("outbox.parent.spanId", parentContext.SpanId.ToString());

            // Proper linking: batch links to message
            if (batchActivity != null)
                batchActivity.AddLink(new ActivityLink(activity.Context));

            return activity;
        }
        catch
        {
            return null;
        }
    }
}
