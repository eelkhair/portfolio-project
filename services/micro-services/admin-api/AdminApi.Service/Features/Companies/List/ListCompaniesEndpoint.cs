using System.Diagnostics;
using AdminAPI.Contracts.Services;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Companies.List;

public class ListCompaniesEndpoint(ICompanyQueryService service) : EndpointWithoutRequest<ApiResponse<List<CompanyResponse>>>
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
        var companies = await service.ListAsync(ct);
        await Send.OkAsync( companies , cancellation: ct);
    }
}
