using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;

namespace JobBoard.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository
{
    Task<User?> FindUserByExternalIdAsync(string externalId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task AddCompanyUser(UserCompany companyUser, CancellationToken cancellationToken);
}