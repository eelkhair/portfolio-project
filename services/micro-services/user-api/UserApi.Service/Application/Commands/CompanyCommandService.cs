using Microsoft.EntityFrameworkCore;
using UserApi.Application.Commands.Interfaces;
using UserApi.Infrastructure.Data;
using UserApi.Infrastructure.Data.Entities;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Application.Commands;

public partial class CompanyCommandService(IUserDbContext context, ILogger<CompanyCommandService> logger) : ICompanyCommandService
{
    public async Task<int> CreateUser(CreateUserRequest request, string userId, CancellationToken ct)
    {
        LogCreatingUser(logger, request.Email);

        var existing = await context.Users.SingleOrDefaultAsync(c => c.Email == request.Email, ct);
        if (existing is not null)
        {
            LogUserAlreadyExists(logger, request.Email, existing.Id);
            return existing.Id;
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            KeycloakUserId = request.KeycloakId
        };

        if (request.UId != null)
        {
            user.UId = request.UId.Value;
        }
        context.Users.Add(user);
        await context.SaveChangesAsync(userId, ct);

        LogUserCreated(logger, user.Id);
        return user.Id;
    }

    public async Task<int> CreateCompany(CreateCompanyRequest request, string userId, CancellationToken ct)
    {
        LogCreatingCompany(logger, request.Name, request.UId);

        var company = new Company
        {
            Name = request.Name,
            KeycloakGroupId = request.KeycloakGroupId,
            UId = request.UId
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync(userId, ct);

        LogCompanyCreated(logger, company.Id, company.UId);
        return company.Id;

    }

    public Task AddUserToCompany(int userId, int companyId, string createdBy, Guid? userCompanyUId,
        CancellationToken ct)
    {
        LogAddingUserToCompany(logger, userId, companyId);

        var userCompany = new UserCompany
        {
            UserId = userId,
            CompanyId = companyId
        };

        if (userCompanyUId != null)
            userCompany.UId = userCompanyUId.Value;

        context.UserCompanies.Add(userCompany);
        return context.SaveChangesAsync(createdBy, ct);
    }

    [LoggerMessage(LogLevel.Information, "Creating user with email '{Email}'")]
    static partial void LogCreatingUser(ILogger logger, string email);

    [LoggerMessage(LogLevel.Information, "User with email '{Email}' already exists, returning Id {UserId}")]
    static partial void LogUserAlreadyExists(ILogger logger, string email, int userId);

    [LoggerMessage(LogLevel.Information, "User created with Id {UserId}")]
    static partial void LogUserCreated(ILogger logger, int userId);

    [LoggerMessage(LogLevel.Information, "Creating company '{Name}' with UId {CompanyUId}")]
    static partial void LogCreatingCompany(ILogger logger, string name, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Company created with Id {CompanyId}, UId {CompanyUId}")]
    static partial void LogCompanyCreated(ILogger logger, int companyId, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Adding user {UserId} to company {CompanyId}")]
    static partial void LogAddingUserToCompany(ILogger logger, int userId, int companyId);
}
