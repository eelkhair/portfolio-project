using JobBoard.Application.Interfaces.Users;

namespace JobBoard.Infrastructure.HttpClients;

public class AuthorizationForwardingHandler(IUserAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = accessor.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("Authorization", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
