using System.Diagnostics;
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
        Activity.Current?.SetTag("entity.type", "draft");
        Activity.Current?.SetTag("entity.id", request.DraftUId);
        Activity.Current?.SetTag("operation", "delete");

        var isForwardSync = string.Equals(HttpContext.Request.Headers["X-Sync-Source"].FirstOrDefault(), "forward", StringComparison.Ordinal);
        Activity.Current?.SetTag("draft.isForwardSync", isForwardSync);

        await service.DeleteDraftAsync(request.DraftUId, DaprExtensions.CreateUser(request.UserId ?? "system"), ct, publishEvent: !isForwardSync);
        await Send.NoContentAsync(cancellation: ct);
    }
}
