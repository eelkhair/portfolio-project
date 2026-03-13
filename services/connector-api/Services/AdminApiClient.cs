using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models.CompanyCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class AdminApiClient(DaprClient daprClient, ActivitySource activitySource, ILogger<AdminApiClient> logger)
    : IAdminApiClient
{
    public async Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("admin-api.SendCompanyCreatedAsync");
        logger.LogInformation("Sending company created event to admin-api");
        var message = daprClient.CreateInvokeMethodRequest(HttpMethod.Post, "admin-api", "api/companies");
        message.Content = JsonContent.Create(payload);
        await daprClient.InvokeMethodAsync(message, cancellationToken);
    }
}
