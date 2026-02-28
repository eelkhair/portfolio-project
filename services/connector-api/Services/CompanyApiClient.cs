using System.Diagnostics;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
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

    public Task SendCompanyUpdatedAsync(Guid companyUId, CompanyUpdatedCompanyApiPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("company-api.SendCompanyUpdatedAsync");
        logger.LogInformation("Sending company updated event to company-api for {CompanyUId}", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Put, "company-api", $"companies/{companyUId}");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }
}