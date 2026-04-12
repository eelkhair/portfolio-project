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

        // Resolve endpoint name: FastEndpoints sets DisplayName to the endpoint class name
        var endpointName = ResolveEndpointName(context, path);
        var entityType = activity?.GetTagItem("entity.type")?.ToString();
        var operation = activity?.GetTagItem("operation")?.ToString();

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["HttpMethod"] = method,
            ["RequestPath"] = path,
            ["EndpointName"] = endpointName,
            ["TraceId"] = activity?.TraceId.ToString()
        }))
        {
            logger.LogInformation("Executing {Endpoint}...", endpointName);

            var sw = Stopwatch.StartNew();
            try
            {
                await next(context);
                sw.Stop();

                // Re-read tags set by the endpoint handler
                entityType ??= activity?.GetTagItem("entity.type")?.ToString();
                operation ??= activity?.GetTagItem("operation")?.ToString();

                var status = context.Response.StatusCode;
                activity?.SetTag("endpoint.duration_ms", sw.ElapsedMilliseconds);

                if (status >= 400)
                {
                    logger.LogWarning("Failed {Endpoint} with {StatusCode} in {Duration}ms",
                        endpointName, status, sw.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogInformation("Successfully executed {Endpoint} ({EntityType}.{Operation}) in {Duration}ms",
                        endpointName, entityType ?? "unknown", operation ?? "unknown", sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("endpoint.duration_ms", sw.ElapsedMilliseconds);

                logger.LogError(ex, "An unexpected failure occurred while executing {Endpoint} after {Duration}ms",
                    endpointName, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static string ResolveEndpointName(HttpContext context, string path)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.DisplayName is { } display && !display.StartsWith("HTTP:"))
        {
            // FastEndpoints sets DisplayName to the fully qualified class name
            // Extract just the class name: "AdminApi.Features.Dashboard.GetDashboardEndpoint" -> "GetDashboardEndpoint"
            var lastDot = display.LastIndexOf('.');
            return lastDot >= 0 ? display[(lastDot + 1)..] : display;
        }

        return $"{context.Request.Method} {path}";
    }
}
