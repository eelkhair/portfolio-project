namespace JobBoard.Domain.Entities.Infrastructure;

public class OutboxArchivedMessage: BaseAuditableEntity
{
    public long OutboxMessageId { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int RetryCount { get; init; }
    public string? LastError { get; init; }
    public string? TraceParent { get; init; } 
}