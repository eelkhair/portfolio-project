using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using JobBoard.Mcp.Common;
using Microsoft.AspNetCore.Builder;

namespace AdminApi.Mcp.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication UseAdminMcpPipeline(this WebApplication app)
    {
        app.UseCors();
        app.UseMiddleware<ForwardedAuthMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        var isAspire = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_MODE"));
        if (!isAspire)
            app.Urls.Add("http://+:3334");

        // Suppress MCP Streamable HTTP SSE probe (GET /) before it generates a trace span
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "GET" && context.Request.Path == "/")
            {
                var activity = System.Diagnostics.Activity.Current;
                if (activity != null)
                {
                    activity.ActivityTraceFlags = System.Diagnostics.ActivityTraceFlags.None;
                    activity.SetStatus(System.Diagnostics.ActivityStatusCode.Unset);
                }
                context.Response.StatusCode = 405;
                return;
            }
            await next();
        });
        app.MapMcp();
        app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
