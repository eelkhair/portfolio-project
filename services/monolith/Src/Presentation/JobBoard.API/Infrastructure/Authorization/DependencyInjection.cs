using HealthChecks.UI.Client;
using JobBoard.API.Infrastructure.SignalR;
using JobBoard.Domain;
using JobBoard.HealthChecks;
using JobBoard.Mcp.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.API.Infrastructure.Authorization;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------------------------------------------------------------------
        // CORS
        // ---------------------------------------------------------------------
        services.AddCors(options =>
        {
            options.AddPolicy("AllowMyFrontendApp", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:4200",
                        "http://localhost:3000",
                        "http://localhost:5280",
                        "https://localhost:5280",
                        "http://127.0.0.1:4200",

                        "https://job-admin-dev.eelkhair.net",
                        "https://jobs-dev.eelkhair.net",
                        "https://job-dev.eelkhair.net",
                        "http://192.168.1.200:9000",
                        "https://swagger-dev.eelkhair.net",

                        "http://192.168.1.112:9000",
                        "https://swagger.eelkhair.net",
                        "https://job-admin.eelkhair.net",
                        "https://job.eelkhair.net",
                        "https://jobs.eelkhair.net",
                        "http://127.0.0.1:5280",

                        "https://job-admin.elkhair.tech",
                        "https://jobs.elkhair.tech",
                        "https://job.elkhair.tech"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("x-trace-id")
                    .AllowCredentials();
            });
        });

        // ---------------------------------------------------------------------
        // JWT Bearer Auth (Keycloak) + InternalApiKey
        // ---------------------------------------------------------------------
        services
            .AddKeycloakJwtAuth(configuration, jwt =>
            {
                // SignalR: read access_token from query string for WebSocket connections
                jwt.Events!.OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    var token = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(token) &&
                        path.StartsWithSegments("/hubs/notifications", StringComparison.Ordinal))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
                "InternalApiKey", _ => { });

        // ---------------------------------------------------------------------
        // Authorization Policies
        // ---------------------------------------------------------------------
        services
            .AddAuthorizationBuilder()

            // Group-based policies (Keycloak groups)
            .AddPolicy(AuthorizationPolicies.Admin, policy =>
                policy.RequireClaim("groups", UserRoles.Admin))

            .AddPolicy(AuthorizationPolicies.Recruiter, policy =>
                policy.RequireClaim("groups", UserRoles.Recruiter))

            .AddPolicy(AuthorizationPolicies.AllUsers, policy =>
                policy.RequireClaim("groups", UserRoles.Admin, UserRoles.Recruiter))

            .AddPolicy(AuthorizationPolicies.Dashboard, policy =>
                policy.RequireClaim("groups", UserRoles.Admin, UserRoles.CompanyAdmin))

            .AddPolicy(AuthorizationPolicies.InternalOrJwt, policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "InternalApiKey")
                    .RequireAuthenticatedUser());

        return services;
    }

    // -------------------------------------------------------------------------
    // Application Middleware Pipeline
    // -------------------------------------------------------------------------
    public static WebApplication UseApplicationServices(this WebApplication app)
    {

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseStaticFiles();
        app.UseODataRouteDebug();
        app.UseRouting();
        app.UseCors("AllowMyFrontendApp");
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    // -------------------------------------------------------------------------
    // Graceful Shutdown for OTEL Providers
    // -------------------------------------------------------------------------
    public static void Start(this WebApplication app)
    {
        var tracerProvider = app.Services.GetService<TracerProvider>();
        var loggerProvider = app.Services.GetService<LoggerProvider>();

        try
        {
            app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);
            app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
            app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();
            app.Run();
        }
        finally
        {
            tracerProvider?.Shutdown();
            loggerProvider?.Shutdown();
        }
    }
}
