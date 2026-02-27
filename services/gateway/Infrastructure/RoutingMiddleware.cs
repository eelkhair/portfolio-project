using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Gateway.Api.Infrastructure;

/// <summary>
/// Sets the x-mode header to route requests to the correct backend (monolith or admin)
/// based on the FeatureFlags:Monolith configuration flag.
/// </summary>
public class RoutingMiddleware(RequestDelegate next, IConfiguration configuration)
{
  public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/ai/v2")
            || context.Request.Path.StartsWithSegments("/dapr/config")
            || context.Request.Path.StartsWithSegments("/dapr/subscribe"))
        {
            if (context.Request.Path.StartsWithSegments("/ai/v2"))
                Activity.Current?.SetTag("service", "AI V2");

            await next(context);
            return;
        }

        var isMonolith = configuration.GetValue<bool>("FeatureFlags:Monolith");


        var mode =  isMonolith ? "monolith" : "admin";
        context.Request.Headers["x-mode"] = mode;
        Activity.Current?.SetTag("service", mode == "monolith" ? "Monolith" : "Admin");

        await next(context);
    }
}
