using Elkhair.Dev.Common.Application;
using UserApi.Infrastructure.Keycloak;

namespace UserApi.Application.Commands.Interfaces;

/// <summary>
/// Self-signup orchestration: creates a Keycloak user with a user-supplied password
/// and adds them to the target top-level group.
/// </summary>
public interface ISignupCommandService
{
    Task<ApiResponse<KeycloakUser>> SignupAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        string groupPath,
        CancellationToken ct,
        string? username = null);
}
