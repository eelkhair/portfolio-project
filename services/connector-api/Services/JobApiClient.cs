using System.Diagnostics;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.Drafts;
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
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "api/companies");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public async Task<JobApiResponse> SendJobCreatedAsync(EventDto<JobCreatedJobApiPayload> payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.SendJobCreatedAsync");
        logger.LogInformation("Sending job created event to job-api");
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "api/jobs");
        message.Content = JsonContent.Create(payload);
        return await client.InvokeMethodAsync<JobApiResponse>(message, cancellationToken);
    }

    public Task SendCompanyUpdatedAsync(Guid companyUId, EventDto<CompanyUpdatedJobApiPayload> payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.SendCompanyUpdatedAsync");
        logger.LogInformation("Sending company updated event to job-api for {CompanyUId}", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Put, "job-api", $"api/companies/{companyUId}");
        message.Content = JsonContent.Create(payload);
        return client.InvokeMethodAsync(message, cancellationToken);
    }

    public async Task<DraftResponse> SaveDraftAsync(Guid companyUId, EventDto<SaveDraftPayload> payload, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.SaveDraftAsync");
        logger.LogInformation("Saving draft to job-api for company {CompanyUId}", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Put, "job-api", $"api/drafts/{companyUId}");
        message.Content = JsonContent.Create(payload);
        return await client.InvokeMethodAsync<DraftResponse>(message, cancellationToken);
    }

    public async Task<List<DraftResponse>> ListDraftsAsync(Guid companyUId, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.ListDraftsAsync");
        logger.LogInformation("Listing drafts from job-api for company {CompanyUId}", companyUId);
        return await client.InvokeMethodAsync<List<DraftResponse>>(HttpMethod.Get, "job-api", $"api/drafts/{companyUId}", cancellationToken);
    }

    public async Task DeleteDraftAsync(Guid draftUId, string userId, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.DeleteDraftAsync");
        logger.LogInformation("Deleting draft {DraftUId} from job-api", draftUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Delete, "job-api", $"api/drafts/{draftUId}");
        message.Content = JsonContent.Create(new { UserId = userId });
        await client.InvokeMethodAsync(message, cancellationToken);
    }

    public async Task<DraftResponse?> GetDraftAsync(Guid draftUId, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("job-api.GetDraftAsync");
        logger.LogInformation("Getting draft {DraftUId} from job-api", draftUId);
        return await client.InvokeMethodAsync<DraftResponse>(HttpMethod.Get, "job-api", $"api/drafts/detail/{draftUId}", cancellationToken);
    }
}