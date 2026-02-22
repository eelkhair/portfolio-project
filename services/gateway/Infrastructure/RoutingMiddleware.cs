using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Gateway.Api.Infrastructure;

/// <summary>
/// Sets the x-mode header to route requests to the correct backend (monolith or admin)
/// based on the FeatureFlags:Monolith configuration flag.
/// </summary>
public class RoutingMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private static readonly Regex JobsByIdPattern =
        new(@"^/jobs/[0-9a-f-]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        // GET /jobs/{guid} (list jobs) only exists on the admin API
        var adminOnly = isMonolith
            && context.Request.Method == "GET"
            && context.Request.Path.Value is { } p
            && JobsByIdPattern.IsMatch(p);

        var mode = adminOnly ? "admin" : isMonolith ? "monolith" : "admin";
        context.Request.Headers["x-mode"] = mode;
        Activity.Current?.SetTag("service", mode == "monolith" ? "Monolith" : "Admin");

        await next(context);
    }
}
