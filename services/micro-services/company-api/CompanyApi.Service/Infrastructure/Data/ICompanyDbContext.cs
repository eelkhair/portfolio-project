using System.Security.Claims;
using CompanyApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Infrastructure.Data;

public interface ICompanyDbContext
{
    DbSet<Company> Companies { get; set; }
    DbSet<Industry> Industries { get; set; }
    Task<int> SaveChangesAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}