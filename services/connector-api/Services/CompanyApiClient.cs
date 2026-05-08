using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.DemoCompany;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class CompanyApiClient(DaprClient client, ActivitySource activitySource, ILogger<CompanyApiClient> logger) : ICompanyApiClient
{
    public Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload companyApiPayload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("company-api.SendCompanyCreatedAsync");
        logger.LogInformation("Sending company created event to company-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "company-api", "api/companies");
        message.Headers.Add("X-Sync-Source", "forward");
        message.Content = JsonContent.Create(companyApiPayload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public Task SendCompanyUpdatedAsync(Guid companyUId, CompanyUpdatedCompanyApiPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("company-api.SendCompanyUpdatedAsync");
        logger.LogInformation("Sending company updated event to company-api for {CompanyUId}", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Put, "company-api", $"api/companies/{companyUId}");
        message.Headers.Add("X-Sync-Source", "forward");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public Task DeleteCompanyAsync(Guid companyUId, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("company-api.DeleteCompanyAsync");
        activity?.SetTag("company.uid", companyUId);
        logger.LogInformation("Deleting demo company {CompanyUId} from company-api", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Delete, "company-api", $"api/companies/{companyUId}");
        message.Headers.Add("X-Sync-Source", "demo-cleanup");
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public Task ClaimCompanyAsync(Guid companyUId, DemoCompanyClaimedPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("company-api.ClaimCompanyAsync");
        activity?.SetTag("company.uid", companyUId);
        logger.LogInformation("Repointing demo company {CompanyUId} admin in company-api", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "company-api", $"api/companies/{companyUId}/claim");
        message.Headers.Add("X-Sync-Source", "demo-claim");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }
}
