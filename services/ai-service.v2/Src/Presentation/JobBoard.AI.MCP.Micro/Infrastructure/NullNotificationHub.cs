using JobBoard.AI.Application.Interfaces.Notifications;

namespace JobBoard.AI.MCP.Micro.Infrastructure;

public class NullNotificationHub(ILogger<NullNotificationHub> logger) : IAiNotificationHub
{
    public Task SendToUserAsync(string userId, string method, AiNotificationDto payload, CancellationToken ct = default)
    {
        logger.LogInformation("MCP notification suppressed — {Type}: {Title}", payload.Type, payload.Title);
        return Task.CompletedTask;
    }

    public Task SendToAllAsync(string method, AiNotificationDto payload, CancellationToken ct = default)
    {
        logger.LogInformation("MCP broadcast suppressed — {Type}: {Title}", payload.Type, payload.Title);
        return Task.CompletedTask;
    }
}
