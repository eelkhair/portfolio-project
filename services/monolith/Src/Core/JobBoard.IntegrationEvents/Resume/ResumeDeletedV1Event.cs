namespace JobBoard.IntegrationEvents.Resume;

public record ResumeDeletedV1Event(
    Guid ResumeUId
) : IIntegrationEvent
{
    public string EventType => "resume.deleted.v1";
    public required string UserId { get; set; }
}
