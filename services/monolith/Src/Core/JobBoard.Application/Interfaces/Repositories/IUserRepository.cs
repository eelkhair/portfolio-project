using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;

namespace JobBoard.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository
{
    Task<User?> FindUserByIdAsync(string userId, CancellationToken cancellationToken);
    Task<User> FindUserByUIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> FindUserByExternalIdOrIdAsync(string externalId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task AddCompanyUser(UserCompany companyUser, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    
}