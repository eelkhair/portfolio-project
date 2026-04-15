using System.Reflection;
using System.Security.Claims;
using CompanyApi.Application.Commands;
using CompanyApi.Application.Commands.Interfaces;
using CompanyApi.Application.Queries;
using CompanyApi.Application.Queries.Interfaces;
using CompanyApi.Infrastructure.Data;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using JobBoard.Mcp.Common;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using NSwag;

namespace CompanyApi.Infrastructure;

public static class DependencyInjection
{
    internal const string CorsPolicy = "AllowJobAdmin";

    public static IServiceCollection AddCompanyApiServices(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddOpenTelemetryServices(cfg, "company-api");

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(mapsterConfig);

        services.AddFastEndpoints()
            .SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Company API";
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
                    "https://job-admin-dev.eelkhair.net",
                    "https://jobs-dev.eelkhair.net",
                    "https://job-dev.eelkhair.net",
                    "http://192.168.1.200:9000",
                    "https://swagger-dev.eelkhair.net",
                    "https://job-admin.eelkhair.net",
                    "http://192.168.1.112:9000",
                    "https://swagger.eelkhair.net",

                    "https://job-admin.elkhair.tech")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("trace-id"));
        });

        services.AddScoped<ICompanyQueryService, CompanyQueryService>();
        services.AddScoped<IIndustryQueryService, IndustryQueryService>();
        services.AddScoped<ICompanyCommandService, CompanyCommandService>();
        services.AddMessageSender();

        services.AddDbContext<CompanyDbContext>(options =>
        {
            options.UseSqlServer(cfg.GetConnectionString("CompanyDbContext"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Companies");
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                });
        });
        services.AddScoped<ICompanyDbContext, CompanyDbContext>();

        services.AddKeycloakJwtAuth(cfg);
        services.AddAuthorization();
        services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());
        services.AddDaprClient();

        return services;
    }
}
