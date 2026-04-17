using System.Security.Claims;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using JobBoard.Mcp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using NSwag;
using UserApi.Application.Commands;
using UserApi.Application.Commands.Interfaces;
using UserApi.Application.Queries;
using UserApi.Application.Queries.Interfaces;
using UserApi.Infrastructure.Data;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;

namespace UserApi.Infrastructure;

public static class DependencyInjection
{
    internal const string CorsPolicy = "AllowJobAdmin";

    public static IServiceCollection AddUserApiServices(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddOpenTelemetryServices(cfg, "user-api");

        services.AddFastEndpoints()
            .SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "User API";
                    s.Version = "v1";

                    var authority = cfg["Keycloak:Authority"] ?? string.Empty;
                    if (!string.IsNullOrEmpty(authority))
                    {
                        s.AddAuth("oauth2", new OpenApiSecurityScheme
                        {
                            Type = OpenApiSecuritySchemeType.OAuth2,
                            Description = "Keycloak (Authorization Code + PKCE)",
                            Flows = new OpenApiOAuthFlows
                            {
                                AuthorizationCode = new OpenApiOAuthFlow
                                {
                                    AuthorizationUrl = $"{authority}/protocol/openid-connect/auth",
                                    TokenUrl = $"{authority}/protocol/openid-connect/token",
                                    Scopes = new Dictionary<string, string>
(StringComparer.Ordinal)
                                    {
                                        ["openid"] = "OpenID",
                                        ["profile"] = "Profile",
                                        ["email"] = "Email"
                                    }
                                }
                            }
                        });
                    }
                };
            });

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, p => p
                .WithOrigins(
                    "http://localhost:4200",
                    "http://192.168.1.200:9000",
                    "http://192.168.1.112:9000",

                    "https://job-admin.elkhair.tech",
                    "https://job-admin-dev.elkhair.tech",
                    "https://jobs.elkhair.tech",
                    "https://jobs-dev.elkhair.tech",
                    "https://job.elkhair.tech",
                    "https://job-dev.elkhair.tech",
                    "https://swagger.elkhair.tech",
                    "https://swagger-dev.elkhair.tech")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("trace-id"));
        });

        services.AddHttpClient("keycloak");
        services.AddScoped<IKeycloakCommandService, KeycloakCommandService>();
        services.AddScoped<ISignupCommandService, SignupCommandService>();
        services.AddSingleton<IKeycloakTokenService, KeycloakTokenService>();
        services.AddTransient<IKeycloakFactory, DefaultKeycloakFactory>();
        services.AddHostedService<KeycloakTokenStartupService>();
        services.AddMessageSender();

        services.AddDbContext<UserDbContext>(options =>
        {
            options.UseSqlServer(cfg.GetConnectionString("UserDbContext"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Users");
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                });
        });
        services.AddScoped<IUserDbContext, UserDbContext>();
        services.AddScoped<ICompanyCommandService, CompanyCommandService>();
        services.AddScoped<IUserQueryService, UserQueryService>();

        services.AddKeycloakJwtAuth(cfg);
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());
        services.AddDaprClient();

        return services;
    }
}
