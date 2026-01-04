using JobBoard.Domain.Entities;

namespace JobBoard.Application.Interfaces.Repositories;

public interface ICompanyRepository : IRepository
{
    Task<int> GetIndustryIdByUId(Guid uid, CancellationToken cancellationToken);
    Task AddAsync(Company company, CancellationToken cancellationToken);
    
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<bool> IndustryExistsAsync(Guid uid, CancellationToken ct);

    Task<Company> GetCompanyById(Guid companyUId, CancellationToken cancellationToken);
}