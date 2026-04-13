using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace JobBoard.AI.API.Infrastructure.Authorization;

/// <summary>
/// A custom authentication handler that ensures requests are authenticated
/// if they originate from loopback IP addresses (Dapr sidecar) or carry
/// a valid internal API key (non-Daprized services like the monolith).
/// </summary>
public class DaprInternalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ip = Context.Connection.RemoteIpAddress;

        // Dapr sidecar calls arrive from loopback
        if (ip != null && IPAddress.IsLoopback(ip))
            return Success("DaprCron");

        // Non-Daprized internal services authenticate via shared API key
        if (Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
        {
            var expectedKey = configuration["InternalApiKey"];
            if (!string.IsNullOrEmpty(expectedKey) &&
                string.Equals(apiKeyHeader, expectedKey, StringComparison.Ordinal))
                return Success("InternalService");

            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        return Task.FromResult(
            AuthenticateResult.Fail($"Unauthorized IP: {ip}")
        );
    }

    private Task<AuthenticateResult> Success(string name)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, name)],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
