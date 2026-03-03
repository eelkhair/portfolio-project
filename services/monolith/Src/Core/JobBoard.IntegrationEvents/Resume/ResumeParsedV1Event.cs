namespace JobBoard.IntegrationEvents.Resume;

public record ResumeParsedV1Event(
    Guid ResumeUId
) : IIntegrationEvent
{
    public string EventType => "resume.parsed.v1";
    public required string UserId { get; set; }
}
