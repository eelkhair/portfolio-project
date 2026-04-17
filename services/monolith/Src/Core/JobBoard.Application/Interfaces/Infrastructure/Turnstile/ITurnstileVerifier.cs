namespace JobBoard.Application.Interfaces.Infrastructure.Turnstile;

/// <summary>
/// Verifies a Cloudflare Turnstile client-side token via the siteverify endpoint.
/// </summary>
public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct);
}
