namespace JobBoard.Infrastructure.Keycloak;

/// <summary>
/// Acquires and caches a Keycloak service-account access token using client_credentials grant.
/// </summary>
public interface IKeycloakTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct);
}
