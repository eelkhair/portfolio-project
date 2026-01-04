using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class JobApiClient(DaprClient client, ILogger<JobApiClient> logger) : IJobApiClient
{
    public Task SendCompanyCreatedAsync(EventDto<CompanyCreatedJobApiPayload> payload,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending company created event to job-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "companies");
        message.Content= JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }
}