namespace UserAPI.Contracts.Models.Requests;

public class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Auth0Id { get; set; } = string.Empty;
    public Guid? UId { get; set; }
}