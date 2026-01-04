using System.Security.Claims;
using CompanyApi.Infrastructure.Data.Entities;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Domain.Constants;
using Elkhair.Dev.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Infrastructure.Data;

public partial class CompanyDbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Industry> Industries { get; set; }
}

public partial class CompanyDbContext : DbContext, ICompanyDbContext
{
    public CompanyDbContext()
    {
        
    }

    public CompanyDbContext(DbContextOptions options): base(options)
    {
        
    }
    
    public CompanyDbContext(string connectionString) : base(GetOptions(connectionString))
    {
    }
    
    private static DbContextOptions GetOptions(string connectionString)
    {
        return new DbContextOptionsBuilder().UseSqlServer(connectionString)
            .Options;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CompanyDbContext).Assembly);
        modelBuilder.HasDefaultSchema("Company");
        //modelBuilder.SeedData();
    }
    
    public Task<int> SaveChangesAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= user.GetUserId();
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy ??= user.GetUserId();
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy ??= user.GetUserId();
                    break;
                case EntityState.Deleted:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy ??= user.GetUserId();
                    entry.Entity.RecordStatus = RecordStatuses.Deleted;
                    entry.State = EntityState.Modified;
                    break;
            }

        return base.SaveChangesAsync(cancellationToken);
    }

    public void Update(object entity)
    {
        this.Entry(entity).State = EntityState.Modified;
        base.Update(entity);
    }
}