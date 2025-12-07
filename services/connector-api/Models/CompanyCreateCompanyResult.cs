namespace ConnectorAPI.Models;
public class CompanyCreateCompanyResult
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public Guid IndustryId { get; set; }
}

