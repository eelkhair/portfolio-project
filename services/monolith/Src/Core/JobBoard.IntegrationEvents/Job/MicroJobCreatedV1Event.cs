namespace JobBoard.IntegrationEvents.Job;

public record MicroJobCreatedV1Event(
    Guid UId,
    Guid CompanyUId,
    string Title,
    string AboutRole,
    string Location,
    string? SalaryRange,
    string JobType,
    List<string> Responsibilities,
    List<string> Qualifications
) : IIntegrationEvent
{
    public string EventType => "micro.job.created.v1";
    public string UserId { get; set; } = string.Empty;
}
