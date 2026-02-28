namespace JobBoard.IntegrationEvents.Company;

public record CompanyUpdatedV1Event(
    Guid CompanyUId,
    Guid IndustryUId
) : IIntegrationEvent
{
    public string EventType => "company.updated.v1";
    public required string UserId { get; set; }
}
