using Auth0.ManagementApi;
using UserApi.Infrastructure.Auth0.Interfaces;

namespace UserApi.Infrastructure.Auth0;

public class DefaultAuth0Factory(IAuth0TokenService tokenService) : IAuth0Factory
{
    public async Task<IAuth0Resource> GetAuth0ResourceAsync(CancellationToken ct = default)
    {
        var token = await tokenService.GetAccessTokenAsync(ct);
        var domain = "elkhair-dev.us.auth0.com";
        var client = new ManagementApiClient(token, domain);
        return new Auth0Resource(client);
    }
}