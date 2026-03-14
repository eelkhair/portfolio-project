namespace JobBoard.AI.Application.Infrastructure;

/// <summary>
/// AsyncLocal context for per-request user identity in MCP servers.
/// Set by UserContextMiddleware from forwarded headers; read by McpUserAccessor.
/// Falls back to null when no forwarded context exists (e.g., MCP Inspector standalone).
/// </summary>
public static class McpUserRequestContext
{
    private static readonly AsyncLocal<string?> _token = new();
    private static readonly AsyncLocal<string?> _userId = new();
    private static readonly AsyncLocal<string?> _firstName = new();
    private static readonly AsyncLocal<string?> _lastName = new();
    private static readonly AsyncLocal<string?> _email = new();
    private static readonly AsyncLocal<List<string>?> _roles = new();

    public static string? CurrentToken
    {
        get => _token.Value;
        set => _token.Value = value;
    }

    public static string? CurrentUserId
    {
        get => _userId.Value;
        set => _userId.Value = value;
    }

    public static string? CurrentFirstName
    {
        get => _firstName.Value;
        set => _firstName.Value = value;
    }

    public static string? CurrentLastName
    {
        get => _lastName.Value;
        set => _lastName.Value = value;
    }

    public static string? CurrentEmail
    {
        get => _email.Value;
        set => _email.Value = value;
    }

    public static List<string>? CurrentRoles
    {
        get => _roles.Value;
        set => _roles.Value = value;
    }

    public static void Clear()
    {
        _token.Value = null;
        _userId.Value = null;
        _firstName.Value = null;
        _lastName.Value = null;
        _email.Value = null;
        _roles.Value = null;
    }
}
