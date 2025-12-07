using ConnectorAPI.Models;

namespace ConnectorAPI.Services;

public class AdminApiClient(IHttpClientFactory httpClientFactory, ILogger<AdminApiClient> logger)
    : IAdminApiClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("admin-api");
    

    public async Task SendCompanyCreatedAsync(CompanyCreatedPayload payload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending CompanyCreated payload to Admin API for Company {CompanyId}", payload.CompanyId);

        var response = await _httpClient.PostAsJsonAsync(
            "/api/companies/created",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}