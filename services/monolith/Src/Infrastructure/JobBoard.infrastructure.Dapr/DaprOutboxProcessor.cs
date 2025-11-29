using Dapr.Client;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain.Entities.Infrastructure;

namespace JobBoard.infrastructure.Dapr;

public class DaprOutboxMessageProcessor(DaprClient client): IOutboxMessageProcessor

{
    public async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await client.PublishEventAsync("pubsub.kafka", "outbox-events", message.Payload, cancellationToken: cancellationToken);
    }
}