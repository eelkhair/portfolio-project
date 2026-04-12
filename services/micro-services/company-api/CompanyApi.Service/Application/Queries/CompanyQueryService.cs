using CompanyApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyApi.Application.Queries;

public partial class CompanyQueryService(ICompanyDbContext companyDbContext, ILogger<CompanyQueryService> logger)
: ICompanyQueryService
{
    public async Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct)
    {
        List<Company> companies;
        var companiesQuery = companyDbContext.Companies.AsNoTracking().Include(c => c.Industry).AsQueryable();

        var uIds = FilteredUIds(context);
        if (uIds.Count > 0)
        {
            LogListingFilteredCompanies(logger, uIds.Count);
            companies = await companiesQuery.Where(c => uIds.Contains(c.UId)).ToListAsync(ct);
        }
        else
        {
            LogListingAllCompanies(logger);
            companies = await companiesQuery.ToListAsync(ct);
        }

        LogListCompaniesCompleted(logger, companies.Count);
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

    [LoggerMessage(LogLevel.Information, "Listing all companies (admin access)")]
    static partial void LogListingAllCompanies(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Listing companies filtered to {Count} company UIDs")]
    static partial void LogListingFilteredCompanies(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "List companies completed, returned {Count} companies")]
    static partial void LogListCompaniesCompleted(ILogger logger, int count);
}