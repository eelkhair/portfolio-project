namespace UserAPI.Contracts.Models.Requests;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string KeycloakGroupId { get; set; } = string.Empty;
    public required Guid UId { get; set; }
}
