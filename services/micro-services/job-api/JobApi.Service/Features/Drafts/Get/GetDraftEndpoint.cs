using System.Diagnostics;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Drafts.Responses;

namespace JobApi.Features.Drafts.Get;

public class GetDraftRequest
{
    public Guid DraftUId { get; set; }
}

public class GetDraftEndpoint(IDraftQueryService service) : Endpoint<GetDraftRequest, DraftResponse>
{
    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/drafts/detail/{draftUId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetDraftRequest request, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "draft");
        Activity.Current?.SetTag("entity.id", request.DraftUId);
        Activity.Current?.SetTag("operation", "get");

        var response = await service.GetDraftAsync(request.DraftUId, ct);
        if (response is null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }
        await Send.OkAsync(response, cancellation: ct);
    }
}
