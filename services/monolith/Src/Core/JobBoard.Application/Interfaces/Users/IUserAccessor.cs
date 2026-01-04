// ReSharper disable UnusedMemberInSuper.Global
namespace JobBoard.Application.Interfaces.Users;

public interface IUserAccessor
{
    string? UserId { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    List<string> Roles { get; set; }
    string? Token { get; set; }
}