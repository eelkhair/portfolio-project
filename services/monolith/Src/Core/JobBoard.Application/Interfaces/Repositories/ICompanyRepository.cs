using JobBoard.Domain.Entities;

namespace JobBoard.Application.Interfaces.Repositories;

public interface ICompanyRepository : IRepository
{
    Task<int> GetIndustryIdByUId(Guid uid, CancellationToken cancellationToken);
    Task AddAsync(Company company, CancellationToken cancellationToken);
}