using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyApi.Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Queries;

public class CompanyQueryService(ICompanyDbContext companyDbContext, ILogger<CompanyQueryService> logger)
: ICompanyQueryService
{
    public async Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct)
    {
        var companies = await companyDbContext.Companies.AsNoTracking().ToListAsync(ct);
        return companies.Adapt<List<CompanyResponse>>();
    }
}