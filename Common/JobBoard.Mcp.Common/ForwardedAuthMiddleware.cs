using Microsoft.AspNetCore.Http;

namespace JobBoard.Mcp.Common;

/// <summary>
/// Copies X-Forwarded-Authorization → Authorization so the existing JWT Bearer
/// pipeline authenticates the request. The AI service sends the user's JWT in
/// X-Forwarded-Authorization via UserTokenForwardingHandler.
/// </summary>
public class ForwardedAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var forwarded = context.Request.Headers["X-Forwarded-Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                context.Request.Headers["Authorization"] = forwarded;
        }

        await next(context);
    }
}
