using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobBoard.AI.API.Infrastructure.HealthChecks;

internal sealed class AnthropicHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["AI:CLAUDE_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return HealthCheckResult.Unhealthy("Missing AI:CLAUDE_API_KEY configuration.");

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await client.SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy("Anthropic API is reachable and credentials are valid.")
            : HealthCheckResult.Unhealthy($"Anthropic returned {(int)response.StatusCode}.");
    }
}
