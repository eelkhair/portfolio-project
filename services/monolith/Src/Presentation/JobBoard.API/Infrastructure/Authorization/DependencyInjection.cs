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
                        "http://localhost:5280",   
                        "https://localhost:5280",
                        "http://127.0.0.1:4200",
                        "http://192.168.1.112:9000",
                        "https://swagger.eelkhair.net",
                        "https://job-admin.eelkhair.net",
                        "http://127.0.0.1:5280"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("x-trace-id")
                    .AllowCredentials();
            });
        });

        // ---------------------------------------------------------------------
        // JWT Bearer Auth (Auth0)
        // ---------------------------------------------------------------------
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{configuration["Auth0:Domain"]}/";
                options.Audience = configuration["Auth0:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://{configuration["Auth0:Domain"]}/",
                    ValidateAudience = true,
                    ValidAudience = configuration["Auth0:Audience"],
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

            // Role-based policies
            .AddPolicy(AuthorizationPolicies.Admin, policy =>
                policy.RequireRole(UserRoles.LabAdmin))

            .AddPolicy(AuthorizationPolicies.Member, policy =>
                policy.RequireRole(UserRoles.LabMember))

            .AddPolicy(AuthorizationPolicies.AllUsers, policy =>
                policy.RequireRole(UserRoles.LabAdmin, UserRoles.LabMember))

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
