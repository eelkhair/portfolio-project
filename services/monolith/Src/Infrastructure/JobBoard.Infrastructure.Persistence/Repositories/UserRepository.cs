using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Repositories;

// ReSharper disable once UnusedType.Global
public class UserRepository(IJobBoardQueryDbContext context): BaseRepository(context), IUserRepository
{
    public async Task<User?> FindUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.InternalId.ToString() == userId, cancellationToken);
    }

    public async Task<User> FindUserByUIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Users.FirstAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> FindUserByExternalIdOrIdAsync(string externalId, CancellationToken cancellationToken)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId
    || u.InternalId.ToString() == externalId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await Context.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddCompanyUser(UserCompany companyUser, CancellationToken cancellationToken)
    {
        await Context.UserCompanies.AddAsync(companyUser, cancellationToken);
    }

    public async  Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        return await Context.Users.AnyAsync(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue && x.Email == email, ct);
    }
}