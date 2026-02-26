using System.Text.Json;
using Dapr.Client;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain;
using JobBoard.Domain.Entities.Infrastructure;

namespace JobBoard.infrastructure.Dapr;

public class DaprOutboxMessageProcessor(DaprClient client): IOutboxMessageProcessor

{
    public async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var topic = MonolithTopicNames.GetTopicForEventType(message.EventType);
        var payload = JsonSerializer.Deserialize<object>(message.Payload);
        var e = new EventDto<object>(message.CreatedBy, Guid.CreateVersion7().ToString(), payload!)
        {
            EventType = message.EventType
        };
        await client.PublishEventAsync(PubSubNames.RabbitMq, topic, e, cancellationToken: cancellationToken);
    }
}