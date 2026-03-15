using Elkhair.Dev.Common.Application;
using FastEndpoints;
using JobApi.Application.Interfaces;

namespace JobApi.Features.Drafts.Delete;

public class DeleteDraftRequest
{
    public Guid DraftUId { get; set; }
    public string? UserId { get; set; }
}

public class DeleteDraftEndpoint(IDraftCommandService service) : Endpoint<DeleteDraftRequest>
{
    public override void Configure()
    {
        Verbs(Http.DELETE);
        Routes("/drafts/{draftUId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteDraftRequest request, CancellationToken ct)
    {
        await service.DeleteDraftAsync(request.DraftUId, DaprExtensions.CreateUser(request.UserId ?? "system"), ct);
        await Send.NoContentAsync(cancellation: ct);
    }
}
