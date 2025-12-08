using System.Security.Claims;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Domain.Constants;
using Elkhair.Dev.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Infrastructure.Data;

public partial class UserDbContext : DbContext, IUserDbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<UserCompany> UserCompanies { get; set; } = null!;
    
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }


    public UserDbContext(string connectionString) : base(GetOptions(connectionString)) { }

    private static DbContextOptions<UserDbContext> GetOptions(string cs) =>
        new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer(cs)
            .Options;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Users"); // set schema first
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
    }

    public Task<int> SaveChangesAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    entry.Entity.RecordStatus = RecordStatuses.Deleted;
                    entry.State = EntityState.Modified; // soft delete
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public void Update(object entity)
    {
        Entry(entity).State = EntityState.Modified;
        base.Update(entity);
    }
}
