using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class UserApiClient(DaprClient client, ILogger<UserApiClient> logger) : IUserApiClient
{
    public Task<CompanyCreatedUserApiPayload> SendCompanyCreatedAsync(EventDto<CompanyCreatedUserApiPayload> payload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending company created event to user-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "user-api", "companies");
        message.Content= JsonContent.Create(payload);
        return client.InvokeMethodAsync<CompanyCreatedUserApiPayload>(message, cancellationToken);
    }
}