using System.Diagnostics;

namespace JobBoard.API.Infrastructure.Authorization;

/// <summary>
/// Middleware to append TraceId to response headers
/// </summary>
/// <param name="next"></param>
public class TraceIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware to add TraceId to response headers
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (traceId != null)
        {
            context.Response.Headers.Append("X-Trace-Id", traceId);
        }
        await next(context);
    }
}