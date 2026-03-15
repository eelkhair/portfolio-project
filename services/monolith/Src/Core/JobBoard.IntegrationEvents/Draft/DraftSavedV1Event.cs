namespace JobBoard.IntegrationEvents.Draft;

public record DraftSavedV1Event(
    Guid UId,
    Guid CompanyUId,
    string Title,
    string AboutRole,
    string Location,
    string JobType,
    string? SalaryRange,
    string Notes,
    List<string> Responsibilities,
    List<string> Qualifications
) : IIntegrationEvent
{
    public string EventType => "draft.saved.v1";
    public string UserId { get; set; }
}
