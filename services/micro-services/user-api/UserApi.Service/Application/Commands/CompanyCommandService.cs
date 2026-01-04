using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Requests;
using UserApi.Infrastructure.Data;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Application.Commands;

public class CompanyCommandService(IUserDbContext context): ICompanyCommandService
{
    public async Task<int> CreateUser(CreateUserRequest request, string userId, CancellationToken ct)
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

        if (request.UId != null)
        {
            user.UId = request.UId.Value;
        }
        context.Users.Add(user);
        await context.SaveChangesAsync(userId, ct);
        return user.Id;
    }

    public async Task<int> CreateCompany(CreateCompanyRequest request, string userId, CancellationToken ct)
    {
        var company = new Company
        {
            Name = request.Name,
            Auth0OrganizationId = request.Auth0OrganizationId,
            UId = request.UId
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync(userId, ct);
        return company.Id;
        
    }

    public Task AddUserToCompany(int userId, int companyId, string createdBy, Guid? userCompanyUId,
        CancellationToken ct)
    {
        var userCompany = new UserCompany
        {
            UserId = userId,
            CompanyId = companyId
        };
        
        if(userCompanyUId != null) 
            userCompany.UId = userCompanyUId.Value;
        
        context.UserCompanies.Add(userCompany);
        return context.SaveChangesAsync(createdBy, ct);
    }
}