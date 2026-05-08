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

    public async Task DeleteCompanyAsync(Guid companyUId, string userId, CancellationToken ct)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.UId == companyUId, ct);
        if (company is null)
        {
            logger.LogInformation("user-api Company {CompanyUId} not found — already deleted", companyUId);
            return;
        }

        // Pull dependent UserCompany rows + the synthetic user(s) bound to this company
        // so the entire company-scoped sub-graph goes in one transaction.
        var userCompanies = await context.UserCompanies
            .Where(uc => uc.CompanyId == company.Id)
            .ToListAsync(ct);

        var userIds = userCompanies.Select(uc => uc.UserId).Distinct().ToList();
        var users = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        context.UserCompanies.RemoveRange(userCompanies);
        context.Users.RemoveRange(users);
        context.Companies.Remove(company);
        await context.SaveChangesAsync(userId, ct);

        logger.LogInformation(
            "Deleted user-api company {CompanyUId} + {UserCount} user(s)",
            companyUId, users.Count);
    }

    public async Task RepointAdminAsync(Guid companyUId, string newAdminEmail, string newAdminFirstName,
        string newAdminLastName, Guid newAdminUId, string userId, CancellationToken ct)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.UId == companyUId, ct)
            ?? throw new InvalidOperationException(
                $"Cannot claim user-api company {companyUId}: not found");

        // Find or create the new user row pointing at the real Keycloak account.
        var newUser = await context.Users.FirstOrDefaultAsync(u => u.Email == newAdminEmail, ct);
        if (newUser is null)
        {
            newUser = new User
            {
                FirstName = newAdminFirstName,
                LastName = newAdminLastName,
                Email = newAdminEmail,
                UId = newAdminUId
            };
            context.Users.Add(newUser);
            await context.SaveChangesAsync(userId, ct);
        }

        // Drop synthetic admin (demo-{guid}@demo.elkhair.tech) UserCompany links + user rows
        // tied to this company, then add the new user → company link.
        var demoUserIds = await context.Users
            .Where(u => u.Email.EndsWith("@demo.elkhair.tech"))
            .Where(u => context.UserCompanies.Any(uc => uc.UserId == u.Id && uc.CompanyId == company.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (demoUserIds.Count > 0)
        {
            var demoLinks = await context.UserCompanies
                .Where(uc => demoUserIds.Contains(uc.UserId) && uc.CompanyId == company.Id)
                .ToListAsync(ct);
            var demoUsers = await context.Users
                .Where(u => demoUserIds.Contains(u.Id))
                .ToListAsync(ct);

            context.UserCompanies.RemoveRange(demoLinks);
            context.Users.RemoveRange(demoUsers);
        }

        var alreadyLinked = await context.UserCompanies
            .AnyAsync(uc => uc.UserId == newUser.Id && uc.CompanyId == company.Id, ct);
        if (!alreadyLinked)
        {
            context.UserCompanies.Add(new UserCompany
            {
                UserId = newUser.Id,
                CompanyId = company.Id
            });
        }

        await context.SaveChangesAsync(userId, ct);

        logger.LogInformation(
            "Repointed user-api company {CompanyUId} admin → {NewAdminEmail}; removed {DemoCount} synthetic admin(s)",
            companyUId, newAdminEmail, demoUserIds.Count);
    }
}
