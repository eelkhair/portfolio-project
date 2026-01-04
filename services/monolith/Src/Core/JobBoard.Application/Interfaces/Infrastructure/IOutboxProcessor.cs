using JobBoard.Domain.Entities.Infrastructure;

namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IOutboxMessageProcessor
{
    Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken);
}