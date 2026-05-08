using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace JobBoard.API.Infrastructure.Authorization;

public class InternalApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var expectedKey = configuration["InternalApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("InternalApiKey not configured"));

        if (!string.Equals(apiKeyHeader, expectedKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        // Stamp NameIdentifier + sub so IUserAccessor.UserId resolves to a non-empty
        // value. Without it the user-context decorator rejects internal calls (e.g.
        // connector-api's saga reading odata/companies during demo provisioning).
        // Also include synthetic first/last/email so UserSyncService.EnsureUserExistsAsync
        // can create the phantom 'InternalService' user row without value-object validation
        // failing on empty fields.
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "InternalService"),
                new Claim(ClaimTypes.NameIdentifier, "InternalService"),
                new Claim("sub", "InternalService"),
                new Claim(ClaimTypes.GivenName, "Internal"),
                new Claim("given_name", "Internal"),
                new Claim(ClaimTypes.Surname, "Service"),
                new Claim("family_name", "Service"),
                new Claim(ClaimTypes.Email, "internal-service@elkhair.tech"),
                new Claim("email", "internal-service@elkhair.tech")
            },
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
