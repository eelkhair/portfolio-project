using System.Security.Claims;
using CompanyApi.Application.Commands.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Commands;

public class CompanyCommandService(ICompanyDbContext context): ICompanyCommandService
{
    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var company = request.Adapt<Company>();
        company.Status = "Provisioning";
        company.IndustryId = await context.Industries.Where(c => c.UId == request.IndustryUId).Select(c => c.Id)
            .FirstAsync(ct);
        context.Companies.Add(company);

        await context.SaveChangesAsync(user, ct);
        
        return company.Adapt<CompanyResponse>();

    }
}