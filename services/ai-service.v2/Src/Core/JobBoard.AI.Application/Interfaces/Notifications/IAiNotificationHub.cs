namespace JobBoard.AI.Application.Interfaces.Notifications;

public sealed record AiNotificationDto(
    string Type,
    string Title,
    string EntityId,
    string EntityType,
    string? TraceParent,
    string? TraceState,
    string? CorrelationId = null,
    Dictionary<string, object>? Metadata = null,
    DateTimeOffset Timestamp = default
    
);

public interface IAiNotificationHub
{
    Task SendToUserAsync(string userId, string method, AiNotificationDto payload, CancellationToken ct = default);
    Task SendToAllAsync(string method, AiNotificationDto payload, CancellationToken ct = default);
}

public static class AiNotificationMethods
{
    public const string Notification = "ai.notification";
}