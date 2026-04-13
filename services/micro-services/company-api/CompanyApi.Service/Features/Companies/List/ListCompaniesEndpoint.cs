using System.Diagnostics;
using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace CompanyApi.Features.Companies.List;

public class ListCompaniesEndpoint(ICompanyQueryService service) : EndpointWithoutRequest<List<CompanyResponse>>
{
    public override void Configure()
    {
        Get("/companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "company");
        Activity.Current?.SetTag("operation", "list");

        var companies = await service.ListAsync(HttpContext, ct);
        await Send.OkAsync(companies, cancellation: ct);
    }
}
