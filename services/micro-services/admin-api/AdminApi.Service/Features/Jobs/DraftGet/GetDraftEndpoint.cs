using System.Diagnostics;
using AdminAPI.Contracts.Services;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.DraftGet;

public sealed class GetDraftEndpoint(IJobQueryService service) :
    EndpointWithoutRequest<ApiResponse<JobDraftResponse?>>
{
    public override void Configure()
    {
        Get("jobs/drafts/{draftId}");
        Summary(s =>
        {
            s.Summary = "Get a draft by ID";
            s.Description = "Retrieves a single job draft by its ID.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var draftId = Route<Guid>("draftId");
        Activity.Current?.SetTag("entity.type", "draft");
        Activity.Current?.SetTag("entity.id", draftId);
        Activity.Current?.SetTag("operation", "get");
        var response = await service.GetDraft(draftId, ct);
        await Send.OkAsync(response, ct);
    }
}
