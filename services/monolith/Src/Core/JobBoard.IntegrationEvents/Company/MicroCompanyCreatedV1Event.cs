namespace JobBoard.IntegrationEvents.Company;

public record MicroCompanyCreatedV1Event(
    Guid CompanyUId,
    string Name,
    string CompanyEmail,
    string? CompanyWebsite,
    Guid IndustryUId,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    Guid? AdminUId,
    Guid? UserCompanyUId
) : IIntegrationEvent
{
    public string EventType => "micro.company.created.v1";
    public string UserId { get; set; } = string.Empty;
}
