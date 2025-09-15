namespace UserApi.Infrastructure.Auth0.Interfaces;

public interface IAuth0Factory
{
    /// <summary>
    /// Returns an Auth0 resource wrapper that is initialized with a fresh/valid Management API token.
    /// </summary>
    Task<IAuth0Resource> GetAuth0ResourceAsync(CancellationToken ct = default);
}