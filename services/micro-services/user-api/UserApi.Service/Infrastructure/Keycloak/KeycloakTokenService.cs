using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr.Client;
using UserApi.Infrastructure.Keycloak.Interfaces;

namespace UserApi.Infrastructure.Keycloak;

public class KeycloakTokenService(
    IConfiguration configuration,
    DaprClient dapr,
    ActivitySource activitySource,
    IHttpClientFactory httpClientFactory) : IKeycloakTokenService
{
    private const string StateStoreName = StateStores.Redis;
    private const string TokenStateKey = "keycloaktoken";

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("Getting Keycloak token.");
        var cached = await dapr.GetStateAsync<string>(StateStoreName, TokenStateKey, cancellationToken: ct);
        activity?.SetTag("cached", cached is not null);
        activity?.Stop();
        return string.IsNullOrWhiteSpace(cached) ? await RefreshAccessTokenAsync(ct) : cached!;
    }

    public async Task<string> RefreshAccessTokenAsync(CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("Refreshing Keycloak token.");

        var tokenUrl = configuration["Keycloak:TokenUrl"]
                       ?? throw new InvalidOperationException("Missing Keycloak:TokenUrl");
        var clientId = configuration["Keycloak:ServiceClientId"]
                       ?? throw new InvalidOperationException("Missing Keycloak:ServiceClientId");
        var clientSecret = configuration["Keycloak:ServiceClientSecret"]
                           ?? throw new InvalidOperationException("Missing Keycloak:ServiceClientSecret");

        var http = httpClientFactory.CreateClient("keycloak");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
(StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        using var res = await http.PostAsync(tokenUrl, formData, ct);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Keycloak token response was empty.");

        var token = payload.AccessToken;

        // Cache with TTL slightly under actual expiry (subtract 120s as a guard)
        var ttl = Math.Max(60, payload.ExpiresIn - 120);
        await dapr.SaveStateAsync(
            StateStoreName,
            key: TokenStateKey,
            value: token,
            metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = ttl.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            cancellationToken: ct);

        activity?.SetTag("token.length", token.Length);
        activity?.Stop();
        return token;
    }
}
