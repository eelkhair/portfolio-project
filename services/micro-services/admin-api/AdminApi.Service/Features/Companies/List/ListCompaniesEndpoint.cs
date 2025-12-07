using AdminApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Companies.List;

public class ListCompaniesEndpoint(ICompanyQueryService service) : EndpointWithoutRequest<ApiResponse<List<CompanyResponse>>>
{
    public override void Configure()
    {
        Get("/companies");
        Permissions("read:companies");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companies = await service.ListAsync(ct);
        await Send.OkAsync( companies , cancellation: ct);
    }
}
