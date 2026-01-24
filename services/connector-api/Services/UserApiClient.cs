using System.Diagnostics;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class UserApiClient(DaprClient client, ActivitySource activitySource, ILogger<UserApiClient> logger) : IUserApiClient
{
    public Task<CompanyCreatedUserApiPayload> SendCompanyCreatedAsync(EventDto<CompanyCreatedUserApiPayload> payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("user-api.SendCompanyCreatedAsync");
        logger.LogInformation("Sending company created event to user-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "user-api", "companies");
        message.Content= JsonContent.Create(payload);
        return client.InvokeMethodAsync<CompanyCreatedUserApiPayload>(message, cancellationToken);
    }
}