using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Infrastructure.Data;
using JobApi.Presentation.Endpoints.Companies.Create;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Features.Companies.List;

public class ListCompaniesEndpoint(IJobDbContext dbContext) : EndpointWithoutRequest<List<CompanyResponse>, CompanyMapper>
{
    public override void Configure()
    {
        Get("/companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companies = await dbContext.Companies.AsNoTracking().ToListAsync(cancellationToken: ct);
        await SendAsync(  companies.Select(Map.FromEntity).ToList(), cancellation: ct);
    }
}
