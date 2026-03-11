using HealthChecks.UI.Client;
using JobBoard.API.Infrastructure.SignalR;
using JobBoard.Domain;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
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
                        "http://127.0.0.1:5280"
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
                options.DefaultAuthenticateScheme = "Auto";
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme("Auto", "JWT or Dapr", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                        return JwtBearerDefaults.AuthenticationScheme;

                    // SignalR WebSocket/SSE transports cannot send Authorization headers;
                    // the token is passed as ?access_token= query param instead.
                    if (!string.IsNullOrEmpty(context.Request.Query["access_token"]) &&
                        context.Request.Path.StartsWithSegments("/hubs"))
                        return JwtBearerDefaults.AuthenticationScheme;

                    return "DaprInternalScheme";
                };
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.Authority = configuration["Keycloak:Authority"];
                options.Audience = configuration["Keycloak:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
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
                            path.StartsWithSegments("/hubs/notifications"))
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

            // Group-based policies (Keycloak groups)
            .AddPolicy(AuthorizationPolicies.Admin, policy =>
                policy.RequireClaim("groups", UserRoles.Admin))

            .AddPolicy(AuthorizationPolicies.Recruiter, policy =>
                policy.RequireClaim("groups", UserRoles.Recruiter))

            .AddPolicy(AuthorizationPolicies.AllUsers, policy =>
                policy.RequireClaim("groups", UserRoles.Admin, UserRoles.Recruiter))

            // -----------------------------------------------------------------
            // Dapr Internal Policy (Dapr → App calls)
            // -----------------------------------------------------------------
            .AddPolicy("DaprInternal", policy =>
            {
                policy.AddAuthenticationSchemes("DaprInternalScheme");
                policy.RequireAuthenticatedUser();
            });

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
            app.UseCloudEvents();
            app.MapSubscribeHandler();
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
