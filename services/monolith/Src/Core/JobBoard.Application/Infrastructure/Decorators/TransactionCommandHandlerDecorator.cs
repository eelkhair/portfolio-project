using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Outbox;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

// ReSharper disable SuspiciousTypeConversion.Global

namespace JobBoard.Application.Infrastructure.Decorators;

public class TransactionCommandHandlerDecorator<TCommand, TResult>(
    IHandler<TCommand, TResult> innerHandler,
    ITransactionDbContext dbContext, 
    IUnitOfWorkEvents unitOfWorkEvents,
    ILogger<TransactionCommandHandlerDecorator<TCommand, TResult>> logger) 
    : IHandler<TCommand, TResult>
    where TCommand : BaseCommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand request, CancellationToken cancellationToken)
    {
        if(request is INoTransaction || dbContext.Database.CurrentTransaction is not null)
        {
            if (typeof(TCommand) != typeof(ProcessOutboxMessageCommand))
            { 
                logger.LogInformation("Skipping database transaction for {CommandName}", typeof(TCommand).Name);
            }
            return await innerHandler.HandleAsync(request, cancellationToken);
        }
        
        logger.LogInformation("Beginning database transaction for command {CommandName}", typeof(TCommand).Name);
        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await innerHandler.HandleAsync(request, cancellationToken);
            logger.LogInformation("Committing database transaction for command {CommandName}", typeof(TCommand).Name);

            await transaction.CommitAsync(cancellationToken);
            await unitOfWorkEvents.ExecuteAndClearAsync();
            return result;
        }
        catch (Exception)
        {
            unitOfWorkEvents.Clear();
            logger.LogError("Rolling back database transaction for command {CommandName}", typeof(TCommand).Name);

            await transaction.RollbackAsync(cancellationToken);
            throw; 
        }
    }
}