using FastEndpoints;
using JobApi.Application.Queries.Interfaces;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Features.Companies.Create;
using JobApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Features.Companies.List;

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
