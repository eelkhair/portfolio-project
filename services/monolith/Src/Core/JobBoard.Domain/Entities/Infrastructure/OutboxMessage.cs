namespace JobBoard.Domain.Entities.Infrastructure;

public class OutboxMessage: BaseAuditableEntity
{
    public required string EventType { get; init; }
    
    public required string Payload { get; init; }
    
    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
    public string? TraceParent { get; set; } 
}