using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace JobBoard.Infrastructure.Keycloak;

/// <summary>
/// Caches the client_credentials token in IMemoryCache with a TTL slightly shorter than its
/// actual expiry. No Dapr dependency — the monolith is intentionally self-contained.
/// </summary>
public class KeycloakTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache) : IKeycloakTokenProvider
{
    private const string CacheKey = "keycloak:admin-token";

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<string>(CacheKey, out var cached) && !string.IsNullOrEmpty(cached))
        {
            return cached!;
        }

        var tokenUrl = configuration["Keycloak:TokenUrl"]
            ?? throw new InvalidOperationException("Missing Keycloak:TokenUrl configuration.");
        var clientId = configuration["Keycloak:ServiceClientId"]
            ?? throw new InvalidOperationException("Missing Keycloak:ServiceClientId configuration.");
        var clientSecret = configuration["Keycloak:ServiceClientSecret"]
            ?? throw new InvalidOperationException("Missing Keycloak:ServiceClientSecret configuration.");

        var http = httpClientFactory.CreateClient("keycloak-admin");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        using var res = await http.PostAsync(tokenUrl, formData, ct);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Keycloak token response was empty.");

        var ttl = TimeSpan.FromSeconds(Math.Max(60, payload.ExpiresIn - 120));
        cache.Set(CacheKey, payload.AccessToken, ttl);
        return payload.AccessToken;
    }
}
