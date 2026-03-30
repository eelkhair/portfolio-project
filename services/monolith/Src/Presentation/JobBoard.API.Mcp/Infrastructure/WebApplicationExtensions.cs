using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using JobBoard.Mcp.Common;
using Microsoft.AspNetCore.Builder;

namespace JobBoard.API.Mcp.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication UseMonolithMcpPipeline(this WebApplication app)
    {
        app.UseCors();
        app.UseMiddleware<ForwardedAuthMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        var isAspire = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_MODE"));
        if (!isAspire)
            app.Urls.Add("http://+:3333");

        app.MapMcp();
        app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
