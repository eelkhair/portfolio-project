using System.Text.Json;
using Dapr.Client;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain.Entities.Infrastructure;

namespace JobBoard.infrastructure.Dapr;

public class DaprOutboxMessageProcessor(DaprClient client): IOutboxMessageProcessor

{
    public async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<object>(message.Payload);
        var e = new EventDto<object>(message.CreatedBy, Guid.CreateVersion7().ToString(), payload!);
        await client.PublishEventAsync("rabbitmq.pubsub", "outbox-events", e , cancellationToken: cancellationToken);
    }
}