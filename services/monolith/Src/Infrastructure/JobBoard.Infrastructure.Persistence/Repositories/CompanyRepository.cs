using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Repositories;

public class CompanyRepository(IJobBoardQueryDbContext context): ICompanyRepository
{
    public async Task<int> GetIndustryIdByUId(Guid uid, CancellationToken cancellationToken)
    {
        return await context.Industries.Where(c => c.UId == uid).Select(c => c.Id)
            .FirstAsync(cancellationToken);
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        await context.Companies.AddAsync(company, cancellationToken);
    }
}