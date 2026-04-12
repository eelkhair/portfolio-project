using Elkhair.Common.Observability;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Builder;

namespace UserApi.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication UseUserApiPipeline(
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
                ui.DocumentTitle = "User API Docs";
                ui.OAuth2Client = new()
                {
                    ClientId = cfg["Keycloak:SwaggerClientId"],
                    AppName = "User API Swagger",
                    UsePkceWithAuthorizationCodeGrant = true
                };
            });
        app.UseSwaggerGen();
        app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
        app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
