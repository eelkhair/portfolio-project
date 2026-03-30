using System.Diagnostics;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Builder;

namespace JobApi.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication UseJobApiPipeline(
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

        app.UseFastEndpoints(c =>
            {
                c.Endpoints.RoutePrefix = "api";
                c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
            })
            .UseSwaggerGen(uiConfig: ui =>
            {
                ui.DocumentTitle = "Job API Docs";
                ui.OAuth2Client = new()
                {
                    ClientId = cfg["Keycloak:SwaggerClientId"],
                    AppName = "Job API Swagger",
                    UsePkceWithAuthorizationCodeGrant = true
                };
            });
        app.UseSwaggerGen();
        app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
        app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
