namespace UserAPI.Contracts.Models.Requests;

public class CreateUserRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebSite { get; set; } = string.Empty;
    public string CompanyUId { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    
}