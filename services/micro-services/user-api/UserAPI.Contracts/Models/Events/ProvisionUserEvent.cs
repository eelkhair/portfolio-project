namespace UserAPI.Contracts.Models.Events;

public class ProvisionUserEvent
{
    public string CompanyName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebSite { get; set; } = string.Empty;
    public Guid CompanyUId { get; set; }
    public string CompanyEmail { get; set; } = string.Empty;
    public Guid? UserCompanyUId { get; set; }
    public Guid? UId { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public string Auth0OrganizationId { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "AdminApi";
}
