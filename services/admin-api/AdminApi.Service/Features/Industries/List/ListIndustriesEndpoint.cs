using AdminApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Industries.Responses;
using FastEndpoints;

namespace AdminApi.Features.Industries.List;


public class ListIndustriesEndpoint(IIndustryQueryService service) : EndpointWithoutRequest<List<IndustryResponse>>
{
    public override void Configure()
    {
        Get("/industries");
        Permissions("read:companies");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var industries = await service.ListAsync(ct);
        await SendAsync( industries , cancellation: ct);
    }
}