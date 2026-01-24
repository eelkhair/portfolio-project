using System.Diagnostics;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models.CompanyCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class CompanyApiClient(DaprClient client, ActivitySource activitySource, ILogger<CompanyApiClient> logger) : ICompanyApiClient
{
    public Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload companyApiPayload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("company-api.SendCompanyCreatedAsync");
        logger.LogInformation("Sending company created event to company-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "company-api", "companies");
        message.Content= JsonContent.Create(companyApiPayload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }
}