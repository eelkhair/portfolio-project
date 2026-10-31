using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobBoard.AI.API.Infrastructure.HealthChecks;

internal sealed class GeminiHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["AI:GEMINI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return HealthCheckResult.Unhealthy("Missing AI:GEMINI_API_KEY configuration.");

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://generativelanguage.googleapis.com/v1/models?key={apiKey}");

        return await ProviderHealth.ProbeAsync("Google Gemini", client, request, cancellationToken);
    }
}
