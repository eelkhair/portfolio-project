using System.Text.Json;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain;
using JobBoard.Domain.Entities.Infrastructure;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Messaging;

public class MassTransitOutboxProcessor(
    CloudEventsPublisher publisher,
    ILogger<MassTransitOutboxProcessor> logger) : IOutboxMessageProcessor
{
    private const string Source = "monolith-api";

    public async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var topic = MonolithTopicNames.GetTopicForEventType(message.EventType);
        var payload = JsonSerializer.Deserialize<object>(message.Payload);

        var eventDto = new
        {
            UserId = message.CreatedBy,
            IdempotencyKey = Guid.CreateVersion7().ToString(),
            Data = payload,
            Created = DateTime.UtcNow,
            EventType = message.EventType
        };

        await publisher.PublishAsync(topic, eventDto, message.EventType, Source, cancellationToken);

        logger.LogInformation(
            "Published outbox message {MessageId} to exchange '{Topic}' as CloudEvent",
            message.Id, topic);
    }
}
