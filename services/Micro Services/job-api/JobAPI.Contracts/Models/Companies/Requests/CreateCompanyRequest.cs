namespace JobAPI.Contracts.Models.Companies.Requests;

public class CreateCompanyRequest
{
    public required string Name { get; set; }
    public required Guid UId { get; set; }
}