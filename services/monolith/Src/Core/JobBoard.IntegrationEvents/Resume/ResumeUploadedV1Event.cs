namespace JobBoard.IntegrationEvents.Resume;

public record ResumeUploadedV1Event(
    Guid ResumeUId,
    string FileName,
    string OriginalFileName,
    string ContentType,
    string? CurrentPage
) : IIntegrationEvent
{
    public string EventType => "resume.uploaded.v1";
    public required string UserId { get; set; }
}
