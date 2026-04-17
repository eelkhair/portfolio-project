namespace AdminApi.Infrastructure.Turnstile;

/// <summary>
/// Verifies a Cloudflare Turnstile client-side token against Cloudflare's siteverify endpoint.
/// </summary>
public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct);
}
