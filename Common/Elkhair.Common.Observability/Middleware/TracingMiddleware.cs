using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elkhair.Common.Observability.Middleware;

public class TracingMiddleware(RequestDelegate next, ILogger<TracingMiddleware> logger)
{
    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/healthzEndpoint", "/liveness", "/health", "/swagger", "/dapr"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var skip = path == "/" || SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        var activity = Activity.Current;
        if (activity is not null)
        {
            var mode = context.Request.Headers["x-mode"].FirstOrDefault();
            if (!string.IsNullOrEmpty(mode))
                activity.SetTag("service", mode == "monolith" ? "Monolith" : "Microservices");
        }

        context.Response.OnStarting(() =>
        {
            var traceId = Activity.Current?.TraceId.ToString();
            if (traceId is not null)
                context.Response.Headers.TryAdd("trace-id", traceId);
            return Task.CompletedTask;
        });

        if (skip)
        {
            await next(context);
            return;
        }

        var method = context.Request.Method;
        var route = context.GetEndpoint()?.DisplayName ?? path;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["HttpMethod"] = method,
            ["RequestPath"] = path,
            ["TraceId"] = activity?.TraceId.ToString()
        }))
        {
            logger.LogInformation("Executing {Method} {Path}", method, path);

            var sw = Stopwatch.StartNew();
            try
            {
                await next(context);
                sw.Stop();

                var status = context.Response.StatusCode;
                activity?.SetTag("endpoint.duration_ms", sw.ElapsedMilliseconds);

                if (status >= 400)
                {
                    logger.LogWarning("Completed {Method} {Path} with {StatusCode} in {Duration}ms",
                        method, path, status, sw.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogInformation("Completed {Method} {Path} with {StatusCode} in {Duration}ms",
                        method, path, status, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("endpoint.duration_ms", sw.ElapsedMilliseconds);

                logger.LogError(ex, "Failed {Method} {Path} after {Duration}ms",
                    method, path, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
