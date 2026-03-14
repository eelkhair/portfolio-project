using AdminAPI.Contracts.Services;
using FastEndpoints;

namespace AdminApi.Features.Jobs.DraftDelete;

public sealed class DeleteDraftEndpoint(IJobCommandService service) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("jobs/{companyId}/drafts/{draftId}");
        Summary(s =>
        {
            s.Summary = "Delete a draft";
            s.Description = "Deletes a job draft by ID.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companyId = Route<string>("companyId")!;
        var draftId = Route<Guid>("draftId");
        await service.DeleteDraft(companyId, draftId, ct);
        await Send.NoContentAsync(cancellation: ct);
    }
}
