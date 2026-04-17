using System.Reflection;
using System.Security.Claims;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using JobApi.Application;
using JobApi.Application.Interfaces;
using JobApi.Features.Jobs.Create;
using JobApi.Infrastructure.Data;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobBoard.Mcp.Common;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using NSwag;

namespace JobApi.Infrastructure;

public static class DependencyInjection
{
    internal const string CorsPolicy = "fe";

    public static IServiceCollection AddJobApiServices(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddOpenTelemetryServices(cfg, "job-api");

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(mapsterConfig);

        services.AddFastEndpoints()
            .SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Job API";
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

        services.AddCors(o =>
        {
            o.AddPolicy(CorsPolicy, p => p
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

        services.AddScoped<IValidator<CreateJobRequest>, CreateJobValidator>();
        services.AddDbContext<JobDbContext>(options =>
        {
            options.UseSqlServer(cfg.GetConnectionString("JobDbContext"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Jobs");
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                });
        });
        services.AddScoped<IJobDbContext, JobDbContext>();
        services.AddScoped<ICompanyCommandService, CompanyCommandService>();
        services.AddScoped<IJobQueryService, JobQueryService>();
        services.AddScoped<IJobCommandService, JobCommandService>();
        services.AddScoped<IDraftCommandService, DraftCommandService>();
        services.AddScoped<IDraftQueryService, DraftQueryService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddMessageSender();

        services.AddKeycloakJwtAuth(cfg);
        services.AddAuthorization();
        services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());
        services.AddDaprClient();

        return services;
    }
}
