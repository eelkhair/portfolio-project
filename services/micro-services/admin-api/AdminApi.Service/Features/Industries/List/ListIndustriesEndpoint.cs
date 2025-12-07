using AdminApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Industries.List;


public class ListIndustriesEndpoint(IIndustryQueryService service) : EndpointWithoutRequest<ApiResponse<List<IndustryResponse>>>
{
    public override void Configure()
    {
        Get("/industries");
        Permissions("read:companies");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var industries = await service.ListAsync(ct);
        await Send.OkAsync( industries , cancellation: ct);
    }
}