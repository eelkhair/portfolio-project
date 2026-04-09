using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Core;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using JobBoard.Mcp.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSwag;

namespace AdminApi.Infrastructure;

public static class DependencyInjection
{
    internal const string CorsPolicy = "AllowJobAdmin";

    public static IServiceCollection AddAdminApiServices(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddOpenTelemetryServices(cfg, "admin-api");
        services.AddMessageSender();
        services.AddStateManager();
        services.AddSignalR();
        services.AddHttpContextAccessor();
        services.AddAdminApiCoreServices();

        services.AddFastEndpoints()
            .SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Admin API";
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
                    "http://192.168.1.200:9000",
                    "https://swagger-dev.eelkhair.net",
                    "https://job-admin.eelkhair.net",
                    "http://192.168.1.112:9000",
                    "https://swagger.eelkhair.net")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("trace-id"));
        });

        services.AddKeycloakJwtAuth(cfg, jwt =>
        {
            jwt.Events!.OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                var token = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(token) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Dashboard", policy =>
                policy.RequireClaim("groups", "Admins", "CompanyAdmins"));
        });

        services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        return services;
    }
}
