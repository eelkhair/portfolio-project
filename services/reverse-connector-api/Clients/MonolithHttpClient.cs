using System.Diagnostics;
using System.Net.Http.Json;
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
}
