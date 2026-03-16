namespace JobBoard.Monolith.Contracts.Sync;

public class SyncCompanyCreateRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public Guid IndustryUId { get; set; }
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public Guid? AdminUId { get; set; }
    public Guid? UserCompanyUId { get; set; }
    public string UserId { get; set; } = "system";
}
