using System.Diagnostics;
using AH.Metadata.Domain.Constants;
using Dapr.Client;
using UserApi.Infrastructure.Auth0.Interfaces;

namespace UserApi.Infrastructure.Auth0;

public class Auth0TokenService(
    IConfiguration configuration,
    DaprClient dapr,
    ActivitySource activitySource,
    IHttpClientFactory httpClientFactory) : IAuth0TokenService
{
    private const string StateStoreName = StateStores.Redis; // ensure this matches your Dapr component
    private const string TokenStateKey  = "auth0token";  // fixed typo from "auto0token"

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("Getting Auth0 token.");
        var cached = await dapr.GetStateAsync<string>(StateStoreName, TokenStateKey, cancellationToken: ct);
        activity?.SetTag("cached", cached is not null);
        activity?.Stop();
        return string.IsNullOrWhiteSpace(cached) ? await RefreshAccessTokenAsync(ct) : cached!;
    }

    public async Task<string> RefreshAccessTokenAsync(CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("Refreshing Auth0 token.");
        var domain       = configuration["Auth0:Domain"]        ?? throw new InvalidOperationException("Missing Auth0:Domain");
        var clientId     = configuration["Auth0:ApiClientId"]   ?? throw new InvalidOperationException("Missing Auth0:ApiClientId");
        var clientSecret = configuration["Auth0:ApiClientSecret"]?? throw new InvalidOperationException("Missing Auth0:ApiClientSecret");
        var audience     = configuration["Auth0:ApiAudience"]   ?? throw new InvalidOperationException("Missing Auth0:ApiAudience");

        var http = httpClientFactory.CreateClient("auth0");
        var req = new Auth0ClientCredentialsRequest
        {
            client_id     = clientId,
            client_secret = clientSecret,
            audience      = audience,
            grant_type    = "client_credentials"
        };

        using var res = await http.PostAsJsonAsync($"https://{domain}/oauth/token", req, ct);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<Auth0TokenResponse>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Auth0 token response was empty.");

        var token = payload.access_token;

        // Cache with TTL slightly under actual expiry (subtract 120s as a guard)
        var ttl = Math.Max(60, payload.expires_in - 120);
        await dapr.SaveStateAsync(
            StateStoreName,
            key: TokenStateKey,
            value: token,
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = ttl.ToString() },
            cancellationToken: ct);
        
        activity?.SetTag("token.length", token.Length);
        activity?.Stop();
        return token;
    }
}
internal sealed class Auth0TokenResponse
{
    public string access_token { get; set; } = "";
    public string token_type   { get; set; } = "";
    public int    expires_in   { get; set; } // seconds
}

internal sealed class Auth0ClientCredentialsRequest
{
    public string client_id     { get; init; } = "";
    public string client_secret { get; init; } = "";
    public string audience      { get; init; } = "";
    public string grant_type    { get; init; } = "client_credentials";
}