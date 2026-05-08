using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.DemoCompany;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class UserApiClient(DaprClient client, ActivitySource activitySource, ILogger<UserApiClient> logger) : IUserApiClient
{
    public Task<CompanyCreatedUserApiPayload> SendCompanyCreatedAsync(EventDto<CompanyCreatedUserApiPayload> payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("user-api.SendCompanyCreatedAsync");
        logger.LogInformation("Sending company created event to user-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "user-api", "api/companies");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync<CompanyCreatedUserApiPayload>(message, cancellationToken);
    }

    public Task DeleteCompanyAsync(Guid companyUId, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("user-api.DeleteCompanyAsync");
        activity?.SetTag("company.uid", companyUId);
        logger.LogInformation("Deleting demo company {CompanyUId} from user-api (Keycloak group + users)", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Delete, "user-api", $"api/companies/{companyUId}");
        message.Headers.Add("X-Sync-Source", "demo-cleanup");
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public Task ClaimCompanyAsync(Guid companyUId, DemoCompanyClaimedPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("user-api.ClaimCompanyAsync");
        activity?.SetTag("company.uid", companyUId);
        logger.LogInformation("Repointing demo company {CompanyUId} admin in user-api → {NewAdminEmail}", companyUId, payload.NewAdminEmail);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "user-api", $"api/companies/{companyUId}/claim");
        message.Headers.Add("X-Sync-Source", "demo-claim");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }
}
