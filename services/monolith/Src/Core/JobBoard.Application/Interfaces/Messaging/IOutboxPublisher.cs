using JobBoard.IntegrationEvents;

namespace JobBoard.Application.Interfaces.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}