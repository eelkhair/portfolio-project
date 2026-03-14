using JobBoard.AI.Application.Infrastructure;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.MCP.Integration.Infrastructure;

/// <summary>
/// MCP server user accessor that reads from AsyncLocal (forwarded from AI service)
/// and falls back to service account identity when no forwarded context exists.
/// </summary>
public class McpUserAccessor(KeycloakTokenService tokenService) : IUserAccessor
{
    public string? UserId
    {
        get => McpUserRequestContext.CurrentUserId ?? "mcp-inspector";
        set { } // Set via middleware AsyncLocal, not directly
    }

    public string? FirstName
    {
        get => McpUserRequestContext.CurrentFirstName ?? "MCP";
        set { }
    }

    public string? LastName
    {
        get => McpUserRequestContext.CurrentLastName ?? "Inspector";
        set { }
    }

    public string? Email
    {
        get => McpUserRequestContext.CurrentEmail ?? "mcp@local";
        set { }
    }

    public List<string> Roles
    {
        get => McpUserRequestContext.CurrentRoles ?? ["Admins"];
        set { }
    }

    public string? Token
    {
        get => McpUserRequestContext.CurrentToken
               ?? tokenService.GetTokenAsync().GetAwaiter().GetResult();
        set { }
    }
}
