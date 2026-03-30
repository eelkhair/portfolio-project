using System.Diagnostics;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Builder;

namespace CompanyApi.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication UseCompanyApiPipeline(
        this WebApplication app,
        IConfiguration cfg)
    {
        app.UseCors(DependencyInjection.CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCloudEvents();
        app.MapSubscribeHandler();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("trace-id", Activity.Current?.TraceId.ToString());
            await next();
        });

        app.UseFastEndpoints(c => { c.Endpoints.RoutePrefix = "api"; })
            .UseSwaggerGen(uiConfig: ui =>
            {
                ui.DocumentTitle = "Company API Docs";
                ui.OAuth2Client = new()
                {
                    ClientId = cfg["Keycloak:SwaggerClientId"],
                    AppName = "Company API Swagger",
                    UsePkceWithAuthorizationCodeGrant = true
                };
            });
        app.UseSwaggerGen();
        app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
        app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
