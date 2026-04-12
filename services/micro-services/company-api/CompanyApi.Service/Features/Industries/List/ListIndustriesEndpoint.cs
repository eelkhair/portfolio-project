using System.Diagnostics;
using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Industries.Responses;
using FastEndpoints;

namespace CompanyApi.Features.Industries.List;

public class ListIndustriesEndpoint(IIndustryQueryService service) : EndpointWithoutRequest<List<IndustryResponse>>
{
    public override void Configure()
    {
        Get("/industries");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "industry");
        Activity.Current?.SetTag("operation", "list");

        var industries = await service.ListAsync(ct);
        await Send.OkAsync( industries , cancellation: ct);
    }
}
