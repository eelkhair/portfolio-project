namespace JobBoard.IntegrationEvents.Company;

public record DemoCompanyClaimedV1Event(
    Guid CompanyUId,
    Guid NewAdminUserUId,
    string NewAdminEmail,
    string NewAdminFirstName,
    string NewAdminLastName,
    Guid SyntheticAdminUserUId
) : IIntegrationEvent
{
    public string EventType => "demo-company.claimed.v1";
    public required string UserId { get; set; }
}
