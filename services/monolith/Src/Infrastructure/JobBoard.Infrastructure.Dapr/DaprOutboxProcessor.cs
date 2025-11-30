using System.Text.Json;
using Dapr.Client;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain.Entities.Infrastructure;

namespace JobBoard.infrastructure.Dapr;

public class DaprOutboxMessageProcessor(DaprClient client): IOutboxMessageProcessor

{
    public async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await client.PublishEventAsync("rabbitmq", "outbox-events", JsonSerializer.Deserialize<object>(message.Payload), cancellationToken: cancellationToken);
    }
}