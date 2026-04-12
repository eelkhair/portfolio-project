using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Elkhair.Common.Observability.Middleware;

public class TracingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            var mode = context.Request.Headers["x-mode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(mode))
                activity.SetTag("service", mode == "monolith" ? "Monolith" : "Microservices");
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            activity?.SetTag("endpoint.duration_ms", sw.ElapsedMilliseconds);
            context.Response.Headers.TryAdd("trace-id", activity?.TraceId.ToString());
        }
    }
}
