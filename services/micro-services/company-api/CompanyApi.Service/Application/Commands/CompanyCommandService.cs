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
        if (request.CompanyId.HasValue)
        {
            company.UId = request.CompanyId.Value;
        }
        
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            company.CreatedBy = request.UserId;
            company.UpdatedBy = request.UserId;
        }
        context.Companies.Add(company);

        await context.SaveChangesAsync(user, ct);

        var response = company.Adapt<CompanyResponse>();
        response.IndustryUId = request.IndustryUId;
        return response;

    }

    public async Task<CompanyResponse> UpdateAsync(Guid companyUId, UpdateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var company = await context.Companies.Include(c => c.Industry).SingleAsync(c => c.UId == companyUId, ct);

        company.Name = request.Name;
        company.Email = request.CompanyEmail;
        company.Website = request.CompanyWebsite;
        company.Phone = request.Phone;
        company.Description = request.Description;
        company.About = request.About;
        company.EEO = request.EEO;
        company.Founded = request.Founded;
        company.Size = request.Size;
        company.Logo = request.Logo;
        company.IndustryId = await context.Industries
            .Where(i => i.UId == request.IndustryUId)
            .Select(i => i.Id)
            .FirstAsync(ct);

        await context.SaveChangesAsync(user, ct);

        var response = company.Adapt<CompanyResponse>();
        response.IndustryUId = request.IndustryUId;
        return response;
    }

    public async Task<bool> ActivateAsync(Guid companyUId, ClaimsPrincipal user, CancellationToken ct)
    {
        var company = await context.Companies.Where(c => c.UId == companyUId).SingleOrDefaultAsync(ct);

        if (company == null)
        {
            return false;
        }
       
        company.Status = "Active";
        context.Companies.Update(company);
        await context.SaveChangesAsync(user, ct);
        return true;
    }
}