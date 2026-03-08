using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobBoard.Monolith.Tests.Integration.Fixtures;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";
    public const string DefaultUserId = "test-user-id";
    public const string DefaultEmail = "test@example.com";
    public const string DefaultFirstName = "Test";
    public const string DefaultLastName = "User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the request explicitly opts out of auth
        if (Request.Headers.ContainsKey("X-Anonymous"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Request.Headers.ContainsKey("x-user-id")
            ? Request.Headers["x-user-id"].ToString()
            : DefaultUserId;

        var claims = new List<Claim>
        {
            new("sub", userId),
            new("email", DefaultEmail),
            new("given_name", DefaultFirstName),
            new("family_name", DefaultLastName),
            new("groups", "Admins"),
            new("groups", "Recruiters"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
