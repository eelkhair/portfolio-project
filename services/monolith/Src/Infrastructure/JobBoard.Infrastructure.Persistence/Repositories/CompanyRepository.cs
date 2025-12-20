using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Repositories;

public class CompanyRepository(IJobBoardQueryDbContext context):BaseRepository(context), ICompanyRepository
{
    public async Task<int> GetIndustryIdByUId(Guid uid, CancellationToken cancellationToken)
    {
       
        return await Context.Industries.Where(c => c.Id == uid).Select(c => c.InternalId)
            .FirstAsync(cancellationToken);
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        await Context.Companies.AddAsync(company, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct)
    {
        return await Context.Companies
            .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue)
            .AnyAsync(x => x.Name == name, ct);    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        return await Context.Companies
            .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue)
            .AnyAsync(x => x.Email == email, ct);
    }

    public async Task<bool> IndustryExistsAsync(Guid uid, CancellationToken ct)
    {
        return await Context.Industries
            .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue)
            .AnyAsync(x => x.Id == uid, ct);
    }

    public async Task<Company> GetCompanyById(Guid companyUId, CancellationToken cancellationToken)
    {
        return await Context.Companies.FirstAsync(c => c.Id == companyUId, cancellationToken);
    }
}