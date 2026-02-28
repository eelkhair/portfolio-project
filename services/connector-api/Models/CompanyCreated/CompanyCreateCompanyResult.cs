namespace ConnectorAPI.Models.CompanyCreated;
public class CompanyCreateCompanyResult
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
    public Guid IndustryUId { get; set; }
}

