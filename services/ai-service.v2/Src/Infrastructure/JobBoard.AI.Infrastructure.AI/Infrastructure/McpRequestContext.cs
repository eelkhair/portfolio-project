namespace JobBoard.AI.Infrastructure.AI.Infrastructure;

/// <summary>
/// AsyncLocal context for flowing the current user's token to MCP HTTP requests.
/// Set by ChatService before LLM invocation; read by UserTokenForwardingHandler.
/// </summary>
public static class McpRequestContext
{
    private static readonly AsyncLocal<string?> _token = new();

    public static string? CurrentToken
    {
        get => _token.Value;
        set => _token.Value = value;
    }
}
