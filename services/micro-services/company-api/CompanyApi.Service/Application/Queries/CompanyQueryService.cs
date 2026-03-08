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
        var groups = context.User.Claims
            .Where(c => c.Type == "groups")
            .Select(c => c.Value.TrimStart('/'))
            .ToList();

        // Admins see all companies
        if (groups.Any(g => g == "Admins"))
            return [];

        // Extract company UIDs from Companies/{uid}/... group paths
        return groups
            .Where(g => g.StartsWith("Companies/"))
            .Select(g => g.Split('/'))
            .Where(parts => parts.Length >= 2 && Guid.TryParse(parts[1], out _))
            .Select(parts => Guid.Parse(parts[1]))
            .Distinct()
            .ToList();
    }
}