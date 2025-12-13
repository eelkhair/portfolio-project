namespace ConnectorAPI.Models.CompanyCreated;

public class CompanyCreatedUserApiPayload
{
    public string CompanyName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid CompanyUId { get; set; }
    public Guid? UserCompanyUId { get; set; }
    public Guid? UId { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public string Auth0OrganizationId { get; set; } = string.Empty;
  
}