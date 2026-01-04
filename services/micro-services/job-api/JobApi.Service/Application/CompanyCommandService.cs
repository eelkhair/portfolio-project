using System.Security.Claims;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Companies.Requests;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;

namespace JobApi.Application;

public class CompanyCommandService(IJobDbContext context): ICompanyCommandService
{
    public async Task CreateCompanyAsync(CreateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var company = new Company
        {
            Name = request.Name,
            UId = request.UId
        };
        await context.Companies.AddAsync(company, ct);
        await context.SaveChangesAsync(user, ct);
    }
}