using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobBoard.AI.API.Infrastructure.HealthChecks;

internal sealed class OpenAiHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["AI:OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return HealthCheckResult.Unhealthy("Missing AI:OPENAI_API_KEY configuration.");

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new("Bearer", apiKey);

        return await ProviderHealth.ProbeAsync("OpenAI", client, request, cancellationToken);
    }
}
