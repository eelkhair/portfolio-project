namespace UserApi.Infrastructure.Auth0.Interfaces;

public interface IAuth0TokenService
{
    /// <summary>Gets a cached access token, refreshing if needed.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>Forces a refresh from Auth0, updates cache, and returns the new token.</summary>
    Task<string> RefreshAccessTokenAsync(CancellationToken ct = default);
}