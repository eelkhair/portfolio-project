using System.Net;
using Elkhair.Dev.Common.Application;
using UserApi.Application.Commands.Interfaces;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;
using ApiError = Elkhair.Dev.Common.Application.ApiError;

namespace UserApi.Application.Commands;

/// <summary>
/// Orchestrates self-signup: creates a Keycloak user with permanent password, then adds them to
/// the target top-level group (e.g. /Admins, /Applicants). Group must already exist.
/// </summary>
public class SignupCommandService(
    IKeycloakFactory keycloakFactory,
    ILogger<SignupCommandService> logger) : ISignupCommandService
{
    public async Task<ApiResponse<KeycloakUser>> SignupAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        string groupPath,
        CancellationToken ct,
        string? username = null)
    {
        var resource = await keycloakFactory.GetKeycloakResourceAsync(ct);

        // Resolve group first — fail fast if the realm is misconfigured.
        var groupName = groupPath.TrimStart('/');
        var group = await resource.FindGroupByNameAsync(groupName, ct);
        if (group is null || string.IsNullOrEmpty(group.Id))
        {
            logger.LogWarning("Signup: target group {GroupPath} not found in Keycloak", groupPath);
            return new ApiResponse<KeycloakUser>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = $"Target group '{groupPath}' not found in Keycloak.",
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["group"] = [$"Group '{groupPath}' does not exist."]
                    }
                }
            };
        }

        var createResult = await resource.CreateUserAsync(
            email, firstName, lastName, attributes: null, ct,
            password: password, username: username);

        if (!createResult.Success || createResult.Data?.Id is null)
        {
            return createResult;
        }

        // If the user already existed (StatusCode.OK instead of Created), signal conflict to the caller.
        if (createResult.StatusCode == HttpStatusCode.OK)
        {
            logger.LogInformation("Signup: email {Email} already exists in Keycloak", email);
            return new ApiResponse<KeycloakUser>
            {
                Success = false,
                StatusCode = HttpStatusCode.Conflict,
                Data = createResult.Data,
                Exceptions = new ApiError
                {
                    Message = "A user with this email already exists.",
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["email"] = ["Already registered."]
                    }
                }
            };
        }

        var addResult = await resource.AddUserToGroupAsync(createResult.Data.Id, group.Id, ct);
        if (!addResult.Success)
        {
            logger.LogError("Signup: created user {UserId} but failed to add to group {GroupPath}",
                createResult.Data.Id, groupPath);
            return new ApiResponse<KeycloakUser>
            {
                Success = false,
                StatusCode = addResult.StatusCode,
                Data = createResult.Data,
                Exceptions = addResult.Exceptions
            };
        }

        logger.LogInformation("Signup: created user {Email} ({UserId}) and added to {GroupPath}",
            email, createResult.Data.Id, groupPath);
        return createResult;
    }
}
