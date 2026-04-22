namespace JobBoard.Application.Interfaces.Infrastructure.Keycloak;

/// <summary>
/// Thin wrapper over Keycloak Admin REST API. Scope limited to what self-signup needs:
/// look up a group by name, create a user with a permanent password, add user to group.
/// Throws <see cref="KeycloakOperationException"/> on HTTP errors or domain conflicts.
/// </summary>
public interface IKeycloakAdminClient
{
    /// <summary>Returns the Keycloak group id for a top-level group name, or null if not found.</summary>
    Task<string?> FindGroupIdByNameAsync(string name, CancellationToken ct);

    /// <summary>
    /// Creates a Keycloak user with a permanent, caller-supplied password and returns the new user id.
    /// The <paramref name="username"/> parameter is optional — when null, the email is used as the Keycloak
    /// username (admin signup behavior). When provided, it is stored as the Keycloak username while email
    /// remains the email address (public signup behavior).
    /// The <paramref name="attributes"/> parameter is optional — when provided, the keys/values are stored
    /// on the Keycloak user (e.g. <c>anonymous=true</c> for guest/tryout accounts).
    /// Throws <see cref="KeycloakOperationException"/> with <see cref="System.Net.HttpStatusCode.Conflict"/>
    /// if the email or username is already registered.
    /// </summary>
    Task<string> CreateUserWithPasswordAsync(
        string email, string firstName, string lastName, string password, CancellationToken ct,
        string? username = null,
        IDictionary<string, List<string>>? attributes = null);

    /// <summary>Adds a user to a group by their Keycloak IDs.</summary>
    Task AddUserToGroupAsync(string userId, string groupId, CancellationToken ct);
}
