using System.Diagnostics;
using System.Text.Json;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Domain.Entities.Infrastructure;
using JobBoard.IntegrationEvents;

namespace JobBoard.Infrastructure.Outbox;

public class OutboxPublisher(IOutboxDbContext dbContext) : IOutboxPublisher
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var message = new OutboxMessage
        {
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent , integrationEvent.GetType()),
            CreatedAt = DateTime.UtcNow,
            TraceParent = Activity.Current?.Id 
        };

        await dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }
}