namespace ReverseConnectorAPI.Models;

public class SyncCompanyUpdatePayload
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public Guid IndustryUId { get; set; }
}
