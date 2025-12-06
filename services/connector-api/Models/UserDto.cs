namespace ConnectorAPI.Models;

public class UserDto : BaseDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; }= null!;
    public string Email { get; set; }= null!;
    public string? ExternalId { get; set; }
}