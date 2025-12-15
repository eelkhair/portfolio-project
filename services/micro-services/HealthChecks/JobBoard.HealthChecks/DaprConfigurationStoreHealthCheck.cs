using JobBoard.HealthChecks.Dtos;
using Microsoft.Extensions.Options;

public class DaprConfigurationStoreHealthCheck(
    DaprClient daprClient,
    IOptionsMonitor<ConfigurationStoreOptions> optionsMonitor,
    string optionsName)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.Get(optionsName);

        try
        {
            var config = await daprClient.GetConfiguration(storeName: options.StoreName, new List<string>(), cancellationToken: cancellationToken).ConfigureAwait(false);


            if (config is null)
            {
                return HealthCheckResult.Unhealthy(
                    $"Dapr configuration store '{options.StoreName}' is unavailable.");
            }

            return HealthCheckResult.Healthy(
                $"Dapr configuration store '{options.StoreName}' is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Dapr configuration store '{options.StoreName}' failed: {ex.Message}");
        }
    }
}