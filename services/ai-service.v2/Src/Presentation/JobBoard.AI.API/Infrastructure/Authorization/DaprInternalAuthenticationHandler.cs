using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace JobBoard.AI.API.Infrastructure.Authorization;

/// <summary>
/// A custom authentication handler that ensures requests are authenticated
/// only if they originate from loopback IP addresses (127.0.0.1 or ::1).
/// Designed for internal Dapr → service calls.
/// </summary>
public class DaprInternalAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// A custom authentication handler that ensures requests are authenticated
    /// only if they originate from loopback IP addresses (127.0.0.1 or ::1).
    /// Designed for internal Dapr → service calls.
    /// </summary>
    public DaprInternalAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TimeProvider timeProvider   // ⭐ REPLACES ISystemClock
    )
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Validates that the incoming request is from a loopback IP.
    /// If so, authenticates it as "DaprCron".
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ip = Context.Connection.RemoteIpAddress;

        if (ip != null && IPAddress.IsLoopback(ip))
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "DaprCron") },
                Scheme.Name
            );

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(
            AuthenticateResult.Fail($"Unauthorized IP: {ip}")
        );
    }
}