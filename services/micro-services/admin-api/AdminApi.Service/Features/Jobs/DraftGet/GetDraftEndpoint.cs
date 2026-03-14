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
        var response = await service.GetDraft(draftId, ct);
        await Send.OkAsync(response, ct);
    }
}
