using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Infrastructure;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JobBoard.Infrastructure.Persistence.Context;

public partial class JobBoardDbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<OutboxArchivedMessage> OutboxArchivedMessages { get; set; }
    public DbSet<OutboxDeadLetter> OutboxDeadLetters { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<Responsibility> Responsibilities { get; set; }
    public DbSet<Industry> Industries { get; set; }
    public DbSet<UserCompany> UserCompanies { get; set; }
    public DbSet<Company> Companies { get; set; }
}

public partial class JobBoardDbContext(DbContextOptions<JobBoardDbContext> options)
    : DbContext(options), IJobBoardDbContext, ITransactionDbContext, IOutboxDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobBoardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            var schemaName = entityType.GetSchema();
            if (string.IsNullOrEmpty(tableName) || string.Equals(schemaName, "outbox", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            var sequenceName = $"{tableName}_Sequence";
        
            modelBuilder.HasSequence<int>(sequenceName);
        }
        
        //modelBuilder.SeedData();
    }
    public Task<int> SaveChangesAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                        entry.Entity.CreatedBy = userId;
                    
                    entry.Entity.UpdatedAt = now;
                    if (string.IsNullOrEmpty(entry.Entity.UpdatedBy))
                        entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    if (string.IsNullOrEmpty(entry.Entity.UpdatedBy))
                        entry.Entity.UpdatedBy = userId;
                    break;
                
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }

        return base.SaveChangesAsync(cancellationToken);
    }
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => Database.BeginTransactionAsync(cancellationToken);

    public async Task<(int id, Guid uid)> GetNextValueFromSequenceAsync(Type entityType,
        CancellationToken cancellationToken)
    {
       
        var et = Model.FindEntityType(entityType);
        if (et == null)
        {
            throw new ArgumentException($"The type '{entityType.Name}' is not a configured entity in this DbContext.", nameof(entityType));
        }
        
        var tableName = et.GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            throw new InvalidOperationException($"Entity type '{entityType.Name}' does not have a configured table name.");
        }

        var sequenceName = $"{tableName}_Sequence";
        
        var connection = Database.GetDbConnection();
        
        await using var command = connection.CreateCommand();
        
        command.Transaction = Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"SELECT NEXT VALUE FOR {sequenceName};";
        
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
    
        var id = (int)(result   ?? throw new InvalidOperationException());
        var uid = Guid.CreateVersion7();
        return (id, uid);
    }
}