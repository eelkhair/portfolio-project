using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Infrastructure.Data;

public interface IUserDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Company> Companies { get; set; }
    DbSet<UserCompany> UserCompanies { get; set; }
    Task<int> SaveChangesAsync(string userId, CancellationToken cancellationToken);
}