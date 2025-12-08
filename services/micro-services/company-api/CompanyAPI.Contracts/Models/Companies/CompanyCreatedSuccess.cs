namespace CompanyAPI.Contracts.Models.Companies;

public class CompanyCreatedSuccess
{
    public string CompanyName { get; set; } = string.Empty;
    public Guid CompanyUId { get; set; }
    public string CompanyEmail { get; set; } = string.Empty;
    public string Auth0CompanyId { get; set; } = string.Empty;
    public string Auth0UserId { get; set; } = string.Empty;
    public Guid UserUId { get; set; }
}