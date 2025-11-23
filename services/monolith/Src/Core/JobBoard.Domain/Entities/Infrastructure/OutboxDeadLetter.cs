
namespace JobBoard.Domain.Entities.Infrastructure;

public class OutboxDeadLetter: BaseAuditableEntity
{
    public long OutboxMessageId { get; init; }
    public required string EventType { get; init; }
    
    public required string Payload { get; init; }

    public string? LastError { get; init; }
    public string? TraceParent { get; init; } 
    public bool IsEmailSent { get; set;  }
}