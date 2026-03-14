using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Drafts.Responses;

namespace JobApi.Features.Drafts.List;

public class ListDraftsRequest
{
    public Guid CompanyUId { get; set; }
}

public class ListDraftsEndpoint(IDraftQueryService service) : Endpoint<ListDraftsRequest, List<DraftResponse>>
{
    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/drafts/{companyUId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListDraftsRequest request, CancellationToken ct)
    {
        var response = await service.ListDraftsAsync(request.CompanyUId, ct);
        await Send.OkAsync(response, cancellation: ct);
    }
}
