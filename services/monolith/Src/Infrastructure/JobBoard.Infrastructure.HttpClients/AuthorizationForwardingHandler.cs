using JobBoard.Mcp.Common;
using Microsoft.Extensions.Configuration;

namespace JobBoard.Infrastructure.HttpClients;

public class AuthorizationForwardingHandler(IUserAccessor accessor, IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Always include internal API key for service-to-service authentication
        var apiKey = configuration["InternalApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        }

        // Also forward the user's JWT when available (for user-context propagation)
        var token = accessor.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("Authorization", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
