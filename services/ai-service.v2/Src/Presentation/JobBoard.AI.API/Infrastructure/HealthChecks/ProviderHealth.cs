using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobBoard.AI.API.Infrastructure.HealthChecks;

/// <summary>
/// Maps an upstream AI provider probe to a health result. Only credential/config problems are
/// Unhealthy — transient upstream errors (408/429/5xx, network blips, timeouts) are Degraded,
/// so a single provider hiccup doesn't flip the whole service to Unhealthy.
/// </summary>
internal static class ProviderHealth
{
    public static async Task<HealthCheckResult> ProbeAsync(
        string provider, HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                   && !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded($"{provider} is temporarily unreachable.", ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"{provider} API is reachable and credentials are valid.");

            var status = (int)response.StatusCode;
            var transient = status >= 500
                            || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;

            return transient
                ? HealthCheckResult.Degraded($"{provider} returned {status} (transient).")
                : HealthCheckResult.Unhealthy($"{provider} returned {status}.");
        }
    }
}
