namespace ConnectorAPI.Models.CompanyCreated;

public class ActivateCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public Guid CompanyUId { get; set; }
    public string CompanyEmail { get; set; } = string.Empty;

    public string KeycloakGroupId { get; set; } = string.Empty;
    public string KeycloakUserId { get; set; } = string.Empty;
    public Guid UserUId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
