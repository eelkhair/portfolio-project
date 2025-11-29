using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net;

namespace JobBoard.API.Infrastructure.Authorization;

/// <summary>
/// A custom authentication handler that ensures requests are authenticated
/// only if they originate from loopback IP addresses (127.0.0.1 or ::1).
/// </summary>
/// <remarks>
/// This handler is specifically designed to secure internal interactions
/// between Dapr and the application by validating the source of the requests.
/// </remarks>
public class DaprInternalAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Provides authentication handling for requests to ensure they originate from loopback IP addresses.
    /// This class is designed to secure internal Dapr interactions by validating request source.
    /// </summary>
    /// <remarks>
    /// Only requests from loopback IPs ('127.0.0.1', '::1') are authorized for access.
    /// </remarks>
    public DaprInternalAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    /// <summary>
    /// Handles the authentication process by validating the incoming request's origin IP address.
    /// Only requests originating from loopback addresses ('127.0.0.1', '::1') are authenticated.
    /// </summary>
    /// <returns>
    /// A task representing the result of the authentication process.
    /// Returns <see cref="AuthenticateResult.Success"/> if the request is authenticated successfully,
    /// or <see cref="AuthenticateResult.Fail"/> if the request is unauthorized.
    /// </returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ip = Context.Connection.RemoteIpAddress;

        // Only allow loopback requests (127.0.0.1 / ::1)
        if (ip != null && IPAddress.IsLoopback(ip))
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "DaprCron")
            }, Scheme.Name);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail(
            $"Unauthorized IP: {ip}"
        ));
    }
}