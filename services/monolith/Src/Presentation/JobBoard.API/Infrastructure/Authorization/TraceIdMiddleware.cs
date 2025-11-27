using System.Diagnostics;

namespace JobBoard.API.Infrastructure.Authorization;

/// <summary>
/// Middleware to append TraceId to response headers.
/// Ensures that the Activity is created before writing the header.
/// </summary>
public class TraceIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Default
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    { 
        var activity = Activity.Current;

        if (activity == null)
        {
            activity = new Activity("HttpRequest").Start();
        }
        context.Response.OnStarting(() =>
        {
            var traceId = activity.TraceId.ToString();
            context.Response.Headers["x-trace-id"] = traceId;
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            if (activity.Parent == null)
                activity.Stop();
        }
    }
}