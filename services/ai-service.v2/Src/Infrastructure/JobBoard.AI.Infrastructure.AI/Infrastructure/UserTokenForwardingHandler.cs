namespace JobBoard.AI.Infrastructure.AI.Infrastructure;

/// <summary>
/// DelegatingHandler that forwards the current user's token to MCP servers
/// via X-Forwarded-Authorization header. Reads from AsyncLocal McpRequestContext.
/// </summary>
public class UserTokenForwardingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = McpRequestContext.CurrentToken;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Authorization", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
