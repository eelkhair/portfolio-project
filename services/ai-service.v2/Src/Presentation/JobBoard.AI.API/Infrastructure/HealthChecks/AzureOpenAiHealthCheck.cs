using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobBoard.AI.API.Infrastructure.HealthChecks;

internal sealed class AzureOpenAiHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["AI:AZURE_API_ENDPOINT"];
        var apiKey = configuration["AI:AZURE_API_KEY"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            return HealthCheckResult.Unhealthy("Missing AI:AZURE_API_ENDPOINT or AI:AZURE_API_KEY configuration.");

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{endpoint.TrimEnd('/')}/openai/models?api-version=2024-10-21");
        request.Headers.Add("api-key", apiKey);

        var response = await client.SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy("Azure OpenAI is reachable and credentials are valid.")
            : HealthCheckResult.Unhealthy($"Azure OpenAI returned {(int)response.StatusCode}.");
    }
}
