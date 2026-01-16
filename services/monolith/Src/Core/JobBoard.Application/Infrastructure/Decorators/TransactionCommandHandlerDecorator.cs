using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Outbox;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

// ReSharper disable SuspiciousTypeConversion.Global

namespace JobBoard.Application.Infrastructure.Decorators;

public sealed class TransactionCommandHandlerDecorator<TCommand, TResult>(
    IHandler<TCommand, TResult> innerHandler,
    IActivityFactory activityFactory,
    ITransactionDbContext dbContext,
    IUnitOfWorkEvents unitOfWorkEvents,
    ILogger<TransactionCommandHandlerDecorator<TCommand, TResult>> logger)
    : IHandler<TCommand, TResult>
    where TCommand : BaseCommand<TResult>
{
    public async Task<TResult> HandleAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            $"{typeof(TCommand).Name}.handle",
            ActivityKind.Internal);

        activity?.SetTag("command.type", typeof(TCommand).Name);
        activity?.SetTag("transaction.skipped",
            request is INoTransaction ||
            dbContext.Database.CurrentTransaction is not null);

        // Skip transaction if explicitly disabled or already active
        if (request is INoTransaction || dbContext.Database.CurrentTransaction is not null)
        {
            if (typeof(TCommand) != typeof(ProcessOutboxMessageCommand))
            {
                logger.LogInformation(
                    "Skipping database transaction for command {CommandName}",
                    typeof(TCommand).Name);
            }

            return await innerHandler.HandleAsync(request, cancellationToken);
        }

        logger.LogInformation(
            "Beginning database transaction for command {CommandName}",
            typeof(TCommand).Name);

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            var result =
                await innerHandler.HandleAsync(request, cancellationToken);

            logger.LogInformation(
                "Committing database transaction for command {CommandName}",
                typeof(TCommand).Name);

            await transaction.CommitAsync(cancellationToken);
            await unitOfWorkEvents.ExecuteAndClearAsync();

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(ex);

            unitOfWorkEvents.Clear();

            logger.LogError(
                ex,
                "Rolling back database transaction for command {CommandName}",
                typeof(TCommand).Name);

            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

