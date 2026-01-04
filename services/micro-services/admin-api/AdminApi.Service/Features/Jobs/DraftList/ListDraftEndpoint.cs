using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.DraftList;

public sealed class ListDraftEndpoint(IJobQueryService service) :
    EndpointWithoutRequest<ApiResponse<List<JobDraftResponse>>>
{
    private const string RouteTemplate = "jobs/{companyId}/list-drafts";

    public override void Configure()
    {
        Get(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Retrieve list of drafts for company";
            s.Description = "List job drafts for company.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companyId = Route<string>("companyId")!;
        var response = await service.ListDrafts(companyId, ct);
        await Send.OkAsync(response, ct);
    }
}