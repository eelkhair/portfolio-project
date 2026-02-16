namespace JobBoard.AI.Application.Interfaces.Notifications;

public interface IAiNotificationHub
{
    Task SendToUserAsync(string userId, string method, AiNotificationDto payload, CancellationToken ct = default);
    Task SendToAllAsync(string method, AiNotificationDto payload, CancellationToken ct = default);
}

public sealed record AiNotificationDto(
    string Type,
    string Title,
    string EntityId,
    string EntityType,
    string? CorrelationId = null,
    DateTimeOffset Timestamp = default
);

public static class AiNotificationMethods
{
    public const string Notification = "ai.notification";
}