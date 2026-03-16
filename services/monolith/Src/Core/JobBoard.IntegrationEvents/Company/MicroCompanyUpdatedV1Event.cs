namespace JobBoard.IntegrationEvents.Company;

public record MicroCompanyUpdatedV1Event(
    Guid CompanyUId,
    string Name,
    string CompanyEmail,
    string? CompanyWebsite,
    string? Phone,
    string? Description,
    string? About,
    string? EEO,
    DateTime? Founded,
    string? Size,
    string? Logo,
    Guid IndustryUId
) : IIntegrationEvent
{
    public string EventType => "micro.company.updated.v1";
    public string UserId { get; set; } = string.Empty;
}
