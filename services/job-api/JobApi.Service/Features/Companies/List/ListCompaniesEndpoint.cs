using Elkhair.Dev.Common.Application;
using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Features.Companies.Create;
using JobApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Features.Companies.List;

public class ListCompaniesEndpoint(IJobDbContext dbContext) : EndpointWithoutRequest<List<CompanyResponse>, CompanyMapper>
{
    public override void Configure()
    {
        Get("/companies");
        Permissions("read:companies");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var httpClient = DaprExtensions.GetHttpClient("user-api", HttpContext);

        var response = await httpClient.GetAsync("users", ct);
        response.EnsureSuccessStatusCode();
        var companies = await dbContext.Companies.AsNoTracking().ToListAsync(cancellationToken: ct);
        await SendAsync(  companies.Select(Map.FromEntity).ToList(), cancellation: ct);
    }
}
