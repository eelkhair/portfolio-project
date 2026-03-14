using System.Text;
using System.Text.Json;
using JobBoard.AI.Application.Infrastructure;

namespace JobBoard.AI.MCP.Integration.Infrastructure;

/// <summary>
/// Reads X-Forwarded-Authorization header from AI service requests,
/// decodes the JWT payload, and sets McpUserRequestContext (AsyncLocal)
/// so McpUserAccessor returns the real user's identity.
/// </summary>
public class UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            try
            {
                var tokenValue = forwarded.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? forwarded
                    : $"Bearer {forwarded}";

                McpUserRequestContext.CurrentToken = tokenValue;

                // Decode JWT payload (no validation — AI API already validated)
                var rawToken = forwarded.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? forwarded["Bearer ".Length..]
                    : forwarded;

                var parts = rawToken.Split('.');
                if (parts.Length >= 2)
                {
                    var payload = parts[1];
                    // Base64Url → Base64
                    payload = payload.Replace('-', '+').Replace('_', '/');
                    switch (payload.Length % 4)
                    {
                        case 2: payload += "=="; break;
                        case 3: payload += "="; break;
                    }

                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("sub", out var sub))
                        McpUserRequestContext.CurrentUserId = sub.GetString();
                    if (root.TryGetProperty("given_name", out var gn))
                        McpUserRequestContext.CurrentFirstName = gn.GetString();
                    if (root.TryGetProperty("family_name", out var fn))
                        McpUserRequestContext.CurrentLastName = fn.GetString();
                    if (root.TryGetProperty("email", out var em))
                        McpUserRequestContext.CurrentEmail = em.GetString();
                    if (root.TryGetProperty("groups", out var groups) && groups.ValueKind == JsonValueKind.Array)
                    {
                        McpUserRequestContext.CurrentRoles = groups.EnumerateArray()
                            .Select(g => g.GetString()?.TrimStart('/') ?? string.Empty)
                            .Where(g => !string.IsNullOrEmpty(g))
                            .ToList();
                    }

                    logger.LogDebug("Forwarded user context: {UserId} ({Email})",
                        McpUserRequestContext.CurrentUserId, McpUserRequestContext.CurrentEmail);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to decode forwarded JWT — falling back to service account");
            }
        }

        try
        {
            await next(context);
        }
        finally
        {
            McpUserRequestContext.Clear();
        }
    }
}
