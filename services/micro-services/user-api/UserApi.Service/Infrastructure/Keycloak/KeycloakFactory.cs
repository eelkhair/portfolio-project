using System.Net.Http.Headers;
using UserApi.Infrastructure.Keycloak.Interfaces;

namespace UserApi.Infrastructure.Keycloak;

public class DefaultKeycloakFactory(
    IKeycloakTokenService tokenService,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IKeycloakFactory
{
    public async Task<IKeycloakResource> GetKeycloakResourceAsync(CancellationToken ct = default)
    {
        var token = await tokenService.GetAccessTokenAsync(ct);
        var authority = configuration["Keycloak:Authority"]
                        ?? throw new InvalidOperationException("Missing Keycloak:Authority");

        // Derive admin API URL from authority:
        // Authority: https://auth.elkhair.tech/realms/job-board-dev
        // Admin URL: https://auth.elkhair.tech/admin/realms/job-board-dev
        var adminUrl = authority.Replace("/realms/", "/admin/realms/");

        var http = httpClientFactory.CreateClient("keycloak");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return new KeycloakResource(http, adminUrl);
    }
}
