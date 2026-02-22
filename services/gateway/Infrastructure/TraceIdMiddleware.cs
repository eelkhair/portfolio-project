namespace Gateway.Api.Infrastructure;

/// <summary>
/// Copies the backend's trace-id response header into x-trace-id for frontend consumption.
/// </summary>
public class TraceIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var traceId = context.Response.Headers["trace-id"].ToString();
            if (!string.IsNullOrEmpty(traceId))
            {
                context.Response.Headers["x-trace-id"] = traceId;
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}
