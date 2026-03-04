using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.Parse;

/// <summary>
/// Orchestrates the full resume-uploaded flow:
/// download blob → parse via <see cref="ParseResumeCommand"/> → notify monolith.
/// </summary>
public class ProcessResumeUploadedCommand(EventDto<ResumeUploadedV1Event> @event) : BaseCommand<Unit>, ISystemCommand
{
    public EventDto<ResumeUploadedV1Event> Event { get; set; } = @event;
}

public class ProcessResumeUploadedCommandHandler(
    IHandlerContext context,
    IBlobStorageService blobStorage,
    IApplicationOrchestrator orchestrator,
    IMonolithApiClient monolithClient,
    IActivityFactory activityFactory) : BaseCommandHandler(context),
    IHandler<ProcessResumeUploadedCommand, Unit>
{
    private const string ResumeContainer = "resumes";

    public async Task<Unit> HandleAsync(ProcessResumeUploadedCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "ProcessResumeUploadedCommandHandler.HandleAsync", ActivityKind.Internal);

        var eventData = request.Event.Data;
        activity?.SetTag("resume.uid", eventData.ResumeUId);
        activity?.SetTag("resume.fileName", eventData.OriginalFileName);

        Logger.LogInformation(
            "Processing resume parse event for {ResumeUId} ({FileName})",
            eventData.ResumeUId, eventData.OriginalFileName);

        try
        {
            // Download resume from blob storage
            var blobBytes = await blobStorage.DownloadAsync(ResumeContainer, eventData.FileName, cancellationToken);
            var base64Content = Convert.ToBase64String(blobBytes);

            // Parse via orchestrator (new DI scope, full decorator pipeline)
            var parseRequest = new ResumeParseRequest
            {
                FileName = eventData.OriginalFileName,
                ContentType = eventData.ContentType,
                FileContent = base64Content
            };

            var parsed = await orchestrator.ExecuteCommandAsync(
                new ParseResumeCommand(parseRequest), cancellationToken);

            // Notify monolith of success
            await monolithClient.NotifyResumeParseCompletedAsync(new ResumeParseCompletedRequest
            {
                ResumeUId = eventData.ResumeUId,
                ParsedContent = parsed,
                UserId = request.Event.UserId,
                CurrentPage = eventData.CurrentPage
            }, cancellationToken);

            Logger.LogInformation("Resume {ResumeUId} parsed successfully", eventData.ResumeUId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse resume {ResumeUId}", eventData.ResumeUId);

            try
            {
                await monolithClient.NotifyResumeParseFailedAsync(new ResumeParseFailedRequest
                {
                    ResumeUId = eventData.ResumeUId,
                    Reason = ex.Message,
                    UserId = request.Event.UserId,
                    CurrentPage = eventData.CurrentPage
                }, cancellationToken);
            }
            catch (Exception callbackEx)
            {
                Logger.LogError(callbackEx,
                    "Failed to notify monolith of parse failure for resume {ResumeUId}",
                    eventData.ResumeUId);
            }
        }

        return Unit.Value;
    }
}
