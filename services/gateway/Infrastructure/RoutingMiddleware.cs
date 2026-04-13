using System.Diagnostics;

namespace Gateway.Api.Infrastructure;

/// <summary>
/// Sets the x-mode header to route requests to the correct backend (monolith or admin)
/// based on the FeatureFlags:Monolith configuration flag.
/// </summary>
public class RoutingMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/ai/v2", StringComparison.Ordinal)
            || context.Request.Path.StartsWithSegments("/dapr/config", StringComparison.Ordinal)
            || context.Request.Path.StartsWithSegments("/dapr/subscribe", StringComparison.Ordinal))
        {
            if (context.Request.Path.StartsWithSegments("/ai/v2", StringComparison.Ordinal))
                Activity.Current?.SetTag("service", "AI V2");

            await next(context);
            return;
        }

        var clientMode = context.Request.Headers["x-mode"].FirstOrDefault();
        var mode = clientMode is "monolith" or "admin"
            ? clientMode
            : configuration.GetValue<bool>("FeatureFlags:Monolith") ? "monolith" : "admin";
        context.Request.Headers["x-mode"] = mode;
        Activity.Current?.SetTag("service", string.Equals(mode, "monolith", StringComparison.Ordinal) ? "Monolith" : "Admin");

        await next(context);
    }
}
