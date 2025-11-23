using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Requests;
using UserApi.Infrastructure.Data;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Application.Commands;

public class CompanyCommandService(IUserDbContext context): ICompanyCommandService
{
    public async Task<int> CreateUser(CreateUserRequest request, ClaimsPrincipal principal, CancellationToken ct)
    {
        var existing = await context.Users.SingleOrDefaultAsync(c=> c.Email == request.Email, ct);
        if (existing is not null)
        {
            return existing.Id;
        }
        
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Auth0UserId = request.Auth0Id
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(principal, ct);
        return user.Id;
    }

    public async Task<int> CreateCompany(CreateCompanyRequest request, ClaimsPrincipal principal, CancellationToken ct)
    {
        var company = new Company
        {
            Name = request.Name,
            Auth0OrganizationId = request.Auth0OrganizationId,
            UId = request.UId
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync(principal, ct);
        return company.Id;
        
    }

    public Task AddUserToCompany(int userId, int companyId, ClaimsPrincipal principal, CancellationToken ct)
    {
        var userCompany = new UserCompany
        {
            UserId = userId,
            CompanyId = companyId
        };
        context.UserCompanies.Add(userCompany);
        return context.SaveChangesAsync(principal, ct);
    }
}