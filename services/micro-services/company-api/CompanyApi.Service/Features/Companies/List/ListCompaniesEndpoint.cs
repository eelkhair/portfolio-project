using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace CompanyApi.Features.Companies.List;

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
        await Send.OkAsync( companies , cancellation: ct);
    }
}
