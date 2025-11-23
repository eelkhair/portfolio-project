// ReSharper disable UnusedMemberInSuper.Global
namespace JobBoard.Application.Interfaces.Users;

public interface IUserAccessor
{
    string? UserId { get; }
    string? FirstName { get; }
    string? LastName { get; }
    string? Email { get; }
    List<string> Roles { get; }
}