using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Repositories;

// ReSharper disable once UnusedType.Global
public class UserRepository(IJobBoardQueryDbContext context): IUserRepository
{
    public async Task<User?> FindUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.InternalId.ToString() == userId, cancellationToken);
    }

    public async Task<User?> FindUserByExternalIdOrIdAsync(string externalId, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId
    || u.InternalId.ToString() == externalId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddCompanyUser(UserCompany companyUser, CancellationToken cancellationToken)
    {
        await context.UserCompanies.AddAsync(companyUser, cancellationToken);
    }

    public async  Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        return await context.Users.AnyAsync(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue && x.Email == email, ct);
    }
}