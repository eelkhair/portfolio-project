using Elkhair.Common.Observability;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace AdminApi.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication UseAdminApiPipeline(
        this WebApplication app,
        IConfiguration cfg)
    {
        app.UseCors(DependencyInjection.CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseTracingMiddleware();
        app.UseCloudEvents();
        app.MapSubscribeHandler();

        app.UseFastEndpoints(c => { c.Endpoints.RoutePrefix = "api"; })
            .UseSwaggerGen(uiConfig: ui =>
            {
                ui.DocumentTitle = "Admin API Docs";
                ui.OAuth2Client = new()
                {
                    ClientId = cfg["Keycloak:SwaggerClientId"],
                    AppName = "Admin API Swagger",
                    UsePkceWithAuthorizationCodeGrant = true
                };
            });
        app.UseSwaggerGen();
        app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();

        app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();
        app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
