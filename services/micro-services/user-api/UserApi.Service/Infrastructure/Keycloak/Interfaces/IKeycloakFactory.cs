namespace UserApi.Infrastructure.Keycloak.Interfaces;

public interface IKeycloakFactory
{
    /// <summary>
    /// Returns a Keycloak resource wrapper initialized with a fresh/valid Admin API token.
    /// </summary>
    Task<IKeycloakResource> GetKeycloakResourceAsync(CancellationToken ct = default);
}
