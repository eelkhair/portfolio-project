using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Repositories;

// ReSharper disable once UnusedType.Global
public class UserRepository(IJobBoardQueryDbContext context): IUserRepository
{
    public async Task<User?> FindUserByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddCompanyUser(UserCompany companyUser, CancellationToken cancellationToken)
    {
        await context.UserCompanies.AddAsync(companyUser, cancellationToken);
    }
}