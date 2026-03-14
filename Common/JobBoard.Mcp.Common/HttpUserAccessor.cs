using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JobBoard.Mcp.Common;

/// <summary>
/// Reads the current user's identity from HttpContext claims.
/// Shared by both REST API and MCP server projects.
/// </summary>
public class HttpUserAccessor : IUserAccessor
{
    public HttpUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;
        if (httpContextAccessor.HttpContext?.Request.Headers.ContainsKey("x-user-id") == true)
        {
            UserId = httpContextAccessor.HttpContext.Request.Headers["x-user-id"];
        }
        else
        {
            UserId = user.FindFirstValue("sub");
        }

        FirstName = user.FindFirstValue("given_name") ?? string.Empty;
        LastName = user.FindFirstValue("family_name") ?? string.Empty;
        Email = user.FindFirstValue("email") ?? string.Empty;
        Roles = user.FindAll("groups").Select(c => c.Value).ToList();
        Token = (httpContextAccessor.HttpContext?.Request.Headers["Authorization"] ?? string.Empty);
    }

    public string? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
    public List<string> Roles { get; set; } = [];
}
