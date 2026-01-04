namespace JobBoard.IntegrationEvents.Company;

public record CompanyCreatedV1Event(
    Guid CompanyUId,
    Guid IndustryUId,
    Guid AdminUId,
    Guid UserCompanyUId
) : IIntegrationEvent
{
    public string EventType => "company.created.v1";
    public required string UserId { get; set; }
  
}

