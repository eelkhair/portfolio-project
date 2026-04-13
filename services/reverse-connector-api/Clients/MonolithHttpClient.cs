using System.Diagnostics;
using System.Text.Json;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Clients;

public class MonolithHttpClient(HttpClient httpClient, ActivitySource activitySource, ILogger<MonolithHttpClient> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SyncDraftAsync(SyncDraftPayload payload, string userId, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("monolith.SyncDraft");
        activity?.SetTag("draft.id", payload.DraftId.ToString());
        activity?.SetTag("draft.companyId", payload.CompanyId.ToString());
        activity?.SetTag("userId", userId);

        logger.LogInformation("Syncing draft {DraftId} for company {CompanyId} to monolith",
            payload.DraftId, payload.CompanyId);

        var request = new
        {
            payload.DraftId,
            payload.CompanyId,
            payload.ContentJson,
            UserId = userId
        };

        var response = await httpClient.PostAsJsonAsync("api/sync/drafts", request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Draft {DraftId} synced to monolith successfully", payload.DraftId);
    }

    public async Task DeleteDraftAsync(Guid draftUId, Guid companyUId, string userId, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("monolith.DeleteDraft");
        activity?.SetTag("draft.id", draftUId.ToString());
        activity?.SetTag("draft.companyId", companyUId.ToString());
        activity?.SetTag("userId", userId);

        logger.LogInformation("Deleting draft {DraftId} for company {CompanyId} from monolith",
            draftUId, companyUId);

        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"api/sync/drafts/{draftUId}?companyId={companyUId}");
        request.Headers.TryAddWithoutValidation("x-user-id", userId);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Draft {DraftId} deleted from monolith successfully", draftUId);
    }

    public async Task SyncCompanyCreateAsync(SyncCompanyCreatePayload payload, string userId, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("monolith.SyncCompanyCreate");
        activity?.SetTag("company.id", payload.CompanyId.ToString());
        activity?.SetTag("company.name", payload.Name);
        activity?.SetTag("userId", userId);

        logger.LogInformation("Syncing company create {CompanyId} ({CompanyName}) to monolith",
            payload.CompanyId, payload.Name);

        var request = new
        {
            payload.CompanyId,
            payload.Name,
            payload.CompanyEmail,
            payload.CompanyWebsite,
            payload.IndustryUId,
            payload.AdminFirstName,
            payload.AdminLastName,
            payload.AdminEmail,
            payload.AdminUId,
            payload.UserCompanyUId,
            UserId = userId
        };

        var response = await httpClient.PostAsJsonAsync("api/sync/companies", request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Company {CompanyId} create synced to monolith successfully", payload.CompanyId);
    }

    public async Task SyncCompanyUpdateAsync(SyncCompanyUpdatePayload payload, string userId, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("monolith.SyncCompanyUpdate");
        activity?.SetTag("company.id", payload.CompanyId.ToString());
        activity?.SetTag("company.name", payload.Name);
        activity?.SetTag("userId", userId);

        logger.LogInformation("Syncing company update {CompanyId} ({CompanyName}) to monolith",
            payload.CompanyId, payload.Name);

        var request = new
        {
            payload.Name,
            payload.CompanyEmail,
            payload.CompanyWebsite,
            payload.Phone,
            payload.Description,
            payload.About,
            payload.EEO,
            payload.Founded,
            payload.Size,
            payload.Logo,
            payload.IndustryUId,
            UserId = userId
        };

        var response = await httpClient.PutAsJsonAsync($"api/sync/companies/{payload.CompanyId}", request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Company {CompanyId} update synced to monolith successfully", payload.CompanyId);
    }

    public async Task SyncJobCreateAsync(SyncJobCreatePayload payload, string userId, CancellationToken ct)
    {
        using var activity = activitySource.StartActivity("monolith.SyncJobCreate");
        activity?.SetTag("job.id", payload.JobId.ToString());
        activity?.SetTag("job.companyId", payload.CompanyId.ToString());
        activity?.SetTag("job.title", payload.Title);
        activity?.SetTag("userId", userId);

        logger.LogInformation("Syncing job create {JobId} ({JobTitle}) to monolith",
            payload.JobId, payload.Title);

        var request = new
        {
            payload.JobId,
            payload.CompanyId,
            payload.Title,
            payload.AboutRole,
            payload.Location,
            payload.SalaryRange,
            payload.JobType,
            payload.Responsibilities,
            payload.Qualifications,
            UserId = userId
        };

        var response = await httpClient.PostAsJsonAsync("api/sync/jobs", request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Job {JobId} create synced to monolith successfully", payload.JobId);
    }
}
