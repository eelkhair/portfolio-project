using System.Diagnostics;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.JobCreated;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class JobApiClient(DaprClient client, ActivitySource activitySource, ILogger<JobApiClient> logger) : IJobApiClient
{
    public Task SendCompanyCreatedAsync(EventDto<CompanyCreatedJobApiPayload> payload,
        CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.SendCompanyCreatedAsync");
        logger.LogInformation("Sending company created event to job-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "companies");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public async Task<JobApiResponse> SendJobCreatedAsync(JobCreatedJobApiPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.SendJobCreatedAsync");
        logger.LogInformation("Sending job created event to job-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "jobs");
        message.Content = JsonContent.Create(payload);
        return await client.InvokeMethodAsync<JobApiResponse>(message, cancellationToken);
    }

    public Task SendCompanyUpdatedAsync(Guid companyUId, CompanyUpdatedJobApiPayload payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.SendCompanyUpdatedAsync");
        logger.LogInformation("Sending company updated event to job-api for {CompanyUId}", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Put, "job-api", $"companies/{companyUId}");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }
}