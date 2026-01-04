namespace ConnectorAPI.Models.CompanyCreated;

public class CompanyCreatedCompanyApiPayload
{
    public Guid CompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string? CompanyWebsite { get; init; }
    public Guid IndustryUId { get; init; }
    public string? UserId { get; set; }
}