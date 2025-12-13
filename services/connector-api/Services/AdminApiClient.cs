using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class AdminApiClient(ILogger<AdminApiClient> logger, DaprClient daprClient)
    : IAdminApiClient
{
    public async Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload payload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending company created event to admin-api");
        var message = daprClient.CreateInvokeMethodRequest(HttpMethod.Post, "admin-api", "companies");
        message.Content= JsonContent.Create(payload);
        await daprClient.InvokeMethodAsync(message, cancellationToken);
    }
}