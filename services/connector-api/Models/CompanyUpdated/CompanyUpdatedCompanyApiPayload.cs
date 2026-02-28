namespace ConnectorAPI.Models.CompanyUpdated;

public class CompanyUpdatedCompanyApiPayload
{
    public string Name { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string? CompanyWebsite { get; init; }
    public string? Phone { get; init; }
    public string? Description { get; init; }
    public string? About { get; init; }
    public string? EEO { get; init; }
    public DateTime? Founded { get; init; }
    public string? Size { get; init; }
    public string? Logo { get; init; }
    public Guid IndustryUId { get; init; }
}
