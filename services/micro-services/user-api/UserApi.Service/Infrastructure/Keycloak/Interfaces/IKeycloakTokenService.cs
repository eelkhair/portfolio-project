namespace UserApi.Infrastructure.Keycloak.Interfaces;

public interface IKeycloakTokenService
{
    /// <summary>Gets a cached access token, refreshing if needed.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>Forces a refresh from Keycloak, updates cache, and returns the new token.</summary>
    Task<string> RefreshAccessTokenAsync(CancellationToken ct = default);
}
