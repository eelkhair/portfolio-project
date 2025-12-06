using JobBoard.Application.Actions.Base;

namespace JobBoard.Application.Actions.Users.Models;

public class UserDto : BaseDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; }= null!;
    public string Email { get; set; }= null!;
    public string? ExternalId { get; set; }
}