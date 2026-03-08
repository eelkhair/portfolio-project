using Elkhair.Dev.Common.Application;

namespace UserApi.Infrastructure.Keycloak.Interfaces;

/// <summary>
/// Thin wrapper over Keycloak Admin REST API with the operations needed for user/company provisioning.
/// </summary>
public interface IKeycloakResource
{
    Task<ApiResponse<KeycloakGroup>> CreateGroupAsync(Guid uid, string name, CancellationToken ct);
    Task<ApiResponse<KeycloakUser>> CreateUserAsync(string email, string firstName, string lastName,
        Dictionary<string, List<string>>? attributes, CancellationToken ct);
    Task<ApiResponse<bool>> AddUserToGroupAsync(string userId, string groupId, CancellationToken ct);
    Task<ApiResponse<KeycloakGroup>> CreateSubGroupAsync(string parentGroupId, string name, CancellationToken ct);
    Task<List<KeycloakGroup>> GetSubGroupsAsync(string parentGroupId, CancellationToken ct);
    Task<KeycloakGroup?> FindGroupByNameAsync(string name, CancellationToken ct);
    Task<KeycloakUser?> FindUserByEmailAsync(string email, CancellationToken ct);
}
