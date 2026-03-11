using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.Options;

namespace JobBoard.HealthChecks;

public class KeycloakHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakOptions> options)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.Authority))
            return HealthCheckResult.Unhealthy("Keycloak authority URL is not configured.");

        try
        {
            var client = httpClientFactory.CreateClient();
            var discoveryUrl = $"{opts.Authority.TrimEnd('/')}/.well-known/openid-configuration";

            var response = await client.GetAsync(discoveryUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return HealthCheckResult.Unhealthy(
                    $"Keycloak returned {(int)response.StatusCode} from OIDC discovery endpoint.");

            var clients = string.Join(", ", opts.ClientIds);
            return HealthCheckResult.Healthy(
                $"Keycloak realm is reachable. Configured clients: {clients}.",
                new Dictionary<string, object>
                {
                    { "authority", opts.Authority },
                    { "clients", opts.ClientIds }
                });
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Keycloak is unreachable: {ex.Message}");
        }
    }
}
