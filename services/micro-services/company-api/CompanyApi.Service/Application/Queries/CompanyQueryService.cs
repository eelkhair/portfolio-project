using System.Text.Json;
using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Queries;

public class CompanyQueryService(ICompanyDbContext companyDbContext)
: ICompanyQueryService
{
    public async Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct)
    {
        List<Company> companies;
        var companiesQuery = companyDbContext.Companies.AsNoTracking().Include(c => c.Industry).AsQueryable();
        
        var uIds = FilteredUIds(context);
        if (uIds.Count > 0)
        {
            companies = await companiesQuery.Where(c => uIds.Contains(c.UId)).ToListAsync(ct);
        }
        else
        {
            companies = await companiesQuery.ToListAsync(ct);
        }
        
        return companies.Adapt<List<CompanyResponse>>();
    }
    
    private static List<Guid> FilteredUIds(HttpContext context)
    {
        var claims = context.User.Claims.Where(c=> c.Type == "https://eelkhair.net/roles").ToList();
        var roles = claims.Select(c => c.Value).Distinct().ToList();
        if (roles.Contains("admin") && roles.Count==1 )
        {
            return new List<Guid>();
        }

        var c = context.User.Claims.First(c => c.Type == "https://eelkhair.net/user_metadata");
        var companyUIds = JsonSerializer.Deserialize<Dictionary<Guid, string>>(c.Value);
        return companyUIds?.Keys?.ToList()?? [];
    }
}