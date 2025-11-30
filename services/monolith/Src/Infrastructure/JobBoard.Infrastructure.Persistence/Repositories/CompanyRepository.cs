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

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct)
    {
        return await context.Companies
            .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue)
            .AnyAsync(x => x.Name == name, ct);    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        return await context.Companies
            .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue)
            .AnyAsync(x => x.Email == email, ct);
    }

    public async Task<bool> IndustryExistsAsync(Guid uid, CancellationToken ct)
    {
        return await context.Industries
            .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue)
            .AnyAsync(x => x.UId == uid, ct);
    }
}