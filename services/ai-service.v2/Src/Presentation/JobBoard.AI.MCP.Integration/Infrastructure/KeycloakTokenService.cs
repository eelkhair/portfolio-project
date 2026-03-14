using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace JobBoard.AI.MCP.Integration.Infrastructure;

public class KeycloakTokenService(IConfiguration configuration, ILogger<KeycloakTokenService> logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTime _expiresAt = DateTime.MinValue;

    public async Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _expiresAt)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken is not null && DateTime.UtcNow < _expiresAt)
                return _cachedToken;

            var tokenUrl = configuration["Keycloak:TokenUrl"];
            var clientId = configuration["Keycloak:ServiceClientId"];
            var clientSecret = configuration["Keycloak:ServiceClientSecret"];

            if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                logger.LogWarning("Keycloak service credentials not configured — Token will be null");
                return null;
            }

            logger.LogInformation("Acquiring Keycloak service-account token from {TokenUrl}", tokenUrl);

            using var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            var response = await httpClient.PostAsync(tokenUrl, content, ct);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
            if (tokenResponse?.AccessToken is null)
            {
                logger.LogError("Keycloak token response missing access_token");
                return null;
            }

            _cachedToken = $"Bearer {tokenResponse.AccessToken}";
            _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

            logger.LogInformation("Keycloak service-account token acquired, expires in {ExpiresIn}s", tokenResponse.ExpiresIn);
            return _cachedToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to acquire Keycloak service-account token");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
