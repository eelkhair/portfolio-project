namespace UserAPI.Contracts.Models.Requests;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Auth0OrganizationId { get; set; }
    public required Guid UId { get; set; }
}