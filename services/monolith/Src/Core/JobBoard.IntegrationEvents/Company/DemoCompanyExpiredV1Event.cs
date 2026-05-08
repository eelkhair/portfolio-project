namespace JobBoard.IntegrationEvents.Company;

public record DemoCompanyExpiredV1Event(
    Guid CompanyUId
) : IIntegrationEvent
{
    public string EventType => "demo-company.expired.v1";
    public required string UserId { get; set; }
}
