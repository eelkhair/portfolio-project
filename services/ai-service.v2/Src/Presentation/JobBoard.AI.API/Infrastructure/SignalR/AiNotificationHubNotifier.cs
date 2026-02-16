using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.AspNetCore.SignalR;

#pragma warning disable CS1591

namespace JobBoard.AI.API.Infrastructure.SignalR;

public class AiNotificationHubNotifier(
    IHubContext<AiNotificationHub> hub,
    IActivityFactory activityFactory,
    ILogger<AiNotificationHubNotifier> log) : IAiNotificationHub
{
    public async Task SendToUserAsync(string userId, string method, AiNotificationDto payload, CancellationToken ct = default)
    {
        using var act = activityFactory.StartActivity("signalr.message.send", ActivityKind.Producer);

        try
        {
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", method);
            act?.SetTag("messaging.operation", "send");
            act?.SetTag("messaging.entity.id", payload.EntityId);
            act?.SetTag("messaging.entity.type", payload.EntityType);
            act?.SetTag("messaging.summary", payload.Type);
            if (!string.IsNullOrWhiteSpace(payload.CorrelationId))
            {
                act?.SetTag("correlation.id", payload.CorrelationId);
            }
            
            act?.SetTag("enduser.id", userId);

            await hub.Clients.Group(userId).SendAsync(method, payload, ct);
            act?.SetTag("messaging.status", "sent");
        }
        catch (Exception ex)
        {
            act?.SetTag("messaging.status", "failed");

            log.LogError(ex, "Failed to send SignalR message '{Method}' to user {UserId}", method, userId);
        }
    }

    public async Task SendToAllAsync(string method, AiNotificationDto payload, CancellationToken ct = default)
    {
        try
        {
            using var act = activityFactory.StartActivity("signalr.message.broadcast", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", method);
            act?.SetTag("messaging.operation", "broadcast");

            await hub.Clients.All.SendAsync(method, payload, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to broadcast SignalR message '{Method}'", method);
        }
    }
}
