using System.Net.Http.Json;
using JobBoard.Application.Interfaces.Infrastructure.Turnstile;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Turnstile;

/// <summary>
/// POSTs to Cloudflare's /turnstile/v0/siteverify endpoint with the configured server secret
/// and the client-submitted token. Returns true iff Cloudflare confirms success.
/// Mirrors apps/landing-next/app/api/contact/route.ts.
/// </summary>
public class TurnstileVerifier(
    HttpClient http,
    IConfiguration configuration,
    ILogger<TurnstileVerifier> logger) : ITurnstileVerifier
{
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Turnstile verify called with empty token");
            return false;
        }

        var secret = configuration["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogError("Turnstile:SecretKey is not configured — failing closed");
            return false;
        }

        try
        {
            var payload = new { secret, response = token, remoteip = remoteIp };
            using var res = await http.PostAsJsonAsync(SiteVerifyUrl, payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile siteverify returned {Status}", res.StatusCode);
                return false;
            }
            var body = await res.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken: ct);
            if (body is null || !body.Success)
            {
                logger.LogWarning("Turnstile verification failed: ip={Ip} errors={@Errors}",
                    remoteIp, body?.ErrorCodes);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile siteverify call threw");
            return false;
        }
    }

    private sealed class SiteVerifyResponse
    {
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
