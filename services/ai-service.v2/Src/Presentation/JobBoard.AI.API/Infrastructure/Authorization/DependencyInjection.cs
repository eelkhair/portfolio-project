using HealthChecks.UI.Client;
using JobBoard.AI.API.Infrastructure.SignalR;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.AI.API.Infrastructure.Authorization;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUserAccessor, HttpUserAccessor>();
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
                        "http://127.0.0.1:5280",
                        "http://192.168.1.200:9000",
                        "http://192.168.1.112:9000",

                        "https://job-admin.elkhair.tech",
                        "https://job-admin-dev.elkhair.tech",
                        "https://jobs.elkhair.tech",
                        "https://jobs-dev.elkhair.tech",
                        "https://job.elkhair.tech",
                        "https://job-dev.elkhair.tech",
                        "https://swagger.elkhair.tech",
                        "https://swagger-dev.elkhair.tech"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("x-trace-id")
                    .AllowCredentials();
            });
        });

        // ---------------------------------------------------------------------
        // JWT Bearer Auth (Keycloak)
        // ---------------------------------------------------------------------
        services
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
                var authority = configuration["Keycloak:Authority"] ?? string.Empty;
                var realmPath = authority.Contains("/realms/")
                    ? authority[authority.IndexOf("/realms/", StringComparison.Ordinal)..]
                    : "/realms/job-board";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers =
                    [
                        $"https://auth.elkhair.tech{realmPath}"
                    ],
                    ValidateAudience = true,
                    ValidateLifetime = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        var token = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) &&
                            path.StartsWithSegments("/hubs/notifications", StringComparison.Ordinal))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Keycloak "Full group path" emits /Admins, /Companies/... — strip leading /
                        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                        if (identity is not null)
                        {
                            var groupClaims = identity.FindAll("groups").ToList();
                            foreach (var claim in groupClaims)
                            {
                                if (claim.Value.StartsWith('/'))
                                {
                                    identity.RemoveClaim(claim);
                                    identity.AddClaim(new System.Security.Claims.Claim("groups", claim.Value.TrimStart('/')));
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            })

            // -----------------------------------------------------------------
            // Dapr Internal API Token Authentication
            // -----------------------------------------------------------------
            .AddScheme<AuthenticationSchemeOptions, DaprInternalAuthenticationHandler>(
                "DaprInternalScheme", _ => { });



        // ---------------------------------------------------------------------
        // Authorization Policies
        // ---------------------------------------------------------------------
        services
            .AddAuthorizationBuilder()

            // -----------------------------------------------------------------
            // Dapr Internal Policy (Dapr → App calls)
            // -----------------------------------------------------------------
            .AddPolicy("DaprInternal", policy =>
            {
                policy.AddAuthenticationSchemes("DaprInternalScheme");
                policy.RequireAuthenticatedUser();
            })

            // -----------------------------------------------------------------
            // Group-based Chat Policies (Keycloak groups)
            // -----------------------------------------------------------------
            .AddPolicy("AdminChat", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("groups", "Admins");
            })
            .AddPolicy("CompanyAdminChat", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("groups", "Admins", "CompanyAdmins");
            })
            .AddPolicy("PublicChat", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("groups", "Admins", "CompanyAdmins", "Applicants");
            })

            // -----------------------------------------------------------------
            // System Admin Policy (global settings, mode switching)
            // -----------------------------------------------------------------
            .AddPolicy("SystemAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("groups", "SystemAdmins");
            });

        return services;
    }

    // -------------------------------------------------------------------------
    // Application Middleware Pipeline
    // -------------------------------------------------------------------------
    public static WebApplication UseApplicationServices(this WebApplication app)
    {

        if (!app.Environment.IsProduction())
            app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<AiNotificationHub>("/hubs/notifications").RequireAuthorization();
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
            app.UseCloudEvents();
            app.MapSubscribeHandler();
            app.Run();
        }
        finally
        {
            tracerProvider?.Shutdown();
            loggerProvider?.Shutdown();
        }
    }
}
