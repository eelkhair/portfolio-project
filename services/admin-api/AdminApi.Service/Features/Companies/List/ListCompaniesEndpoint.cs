using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace AdminApi.Features.Companies.List;

public class ListCompaniesEndpoint(ICompanyQueryService service) : EndpointWithoutRequest<List<CompanyResponse>>
{
    public override void Configure()
    {
        Get("/companies");
        Permissions("read:companies");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companies = await service.ListAsync( HttpContext, ct);
        await SendAsync( companies , cancellation: ct);
    }
}
