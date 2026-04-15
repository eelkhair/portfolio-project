using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace JobBoard.Mcp.Common;

/// <summary>
/// Shared Keycloak JWT Bearer setup used by both REST API and MCP server projects.
/// Includes the OnTokenValidated handler that strips leading '/' from group claims
/// emitted by Keycloak's "Full group path" mapper.
/// </summary>
public static class KeycloakAuthExtensions
{
    /// <summary>
    /// Adds Keycloak JWT Bearer authentication with group-path stripping.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration containing Keycloak:Authority and Keycloak:Audience.</param>
    /// <param name="configureJwt">
    /// Optional callback to further configure JWT options (e.g. SignalR OnMessageReceived handler).
    /// Called after the base Keycloak configuration is applied.
    /// </param>
    public static AuthenticationBuilder AddKeycloakJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        return services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.Authority = configuration["Keycloak:Authority"];
                options.Audience = configuration["Keycloak:Audience"];
                // Accept tokens issued by either domain (eelkhair.net or elkhair.tech)
                var authority = configuration["Keycloak:Authority"] ?? string.Empty;
                var realmPath = authority.Contains("/realms/")
                    ? authority[authority.IndexOf("/realms/", StringComparison.Ordinal)..]
                    : "/realms/job-board";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers =
                    [
                        $"https://auth.eelkhair.net{realmPath}",
                        $"https://auth.elkhair.tech{realmPath}"
                    ],
                    ValidateAudience = true,
                    ValidateLifetime = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Keycloak "Full group path" emits /Admins, /Companies/... — strip leading /
                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        if (identity is not null)
                        {
                            var groupClaims = identity.FindAll("groups").ToList();
                            foreach (var claim in groupClaims)
                            {
                                if (claim.Value.StartsWith('/'))
                                {
                                    identity.RemoveClaim(claim);
                                    identity.AddClaim(new Claim("groups", claim.Value.TrimStart('/')));
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                };

                // Allow caller to add more config (e.g. SignalR OnMessageReceived, InternalApiKey)
                configureJwt?.Invoke(options);
            });
    }
}
