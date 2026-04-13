using System.Security.Claims;
using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobAPI.Contracts.Models.Companies.Requests;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public partial class CompanyCommandService(IJobDbContext context, ILogger<CompanyCommandService> logger) : ICompanyCommandService
{
    public async Task CreateCompanyAsync(CreateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        LogCreatingCompany(logger, request.Name, request.UId);

        var company = new Company
        {
            Name = request.Name,
            UId = request.UId
        };
        await context.Companies.AddAsync(company, ct);
        await context.SaveChangesAsync(user, ct);

        LogCompanyCreated(logger, request.Name, request.UId);
    }

    public async Task UpdateCompanyAsync(Guid companyUId, UpdateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        LogUpdatingCompany(logger, companyUId, request.Name);

        var company = await context.Companies.SingleAsync(c => c.UId == companyUId, ct);
        company.Name = request.Name;
        await context.SaveChangesAsync(user, ct);

        LogCompanyUpdated(logger, companyUId);
    }

    [LoggerMessage(LogLevel.Information, "Creating company '{CompanyName}' with UId {CompanyUId}")]
    static partial void LogCreatingCompany(ILogger logger, string companyName, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Company created: '{CompanyName}' ({CompanyUId})")]
    static partial void LogCompanyCreated(ILogger logger, string companyName, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Updating company {CompanyUId} to name '{CompanyName}'")]
    static partial void LogUpdatingCompany(ILogger logger, Guid companyUId, string companyName);

    [LoggerMessage(LogLevel.Information, "Company updated: {CompanyUId}")]
    static partial void LogCompanyUpdated(ILogger logger, Guid companyUId);
}
