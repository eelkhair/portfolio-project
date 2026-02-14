namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IUserAccessor
{
    string? UserId { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    List<string> Roles { get; set; }
    string? Token { get; set; }
}