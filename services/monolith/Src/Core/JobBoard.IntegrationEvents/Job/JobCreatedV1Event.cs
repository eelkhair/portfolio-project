namespace JobBoard.IntegrationEvents.Job;

public record JobCreatedV1Event(
    Guid UId,
    string Title,
    string AboutRole,
    string Location,
    string? SalaryRange,
    string? DraftId,
    List<string> Responsibilities,
    List<string> Qualifications,
    string JobType
): IIntegrationEvent
{
    public string EventType => "job.created.v1";
    public string UserId { get; set; }
}