using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Infrastructure;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable UnusedMemberInSuper.Global

namespace JobBoard.Application.Interfaces;

public interface IJobBoardQueryDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Job> Jobs { get; set; }
    DbSet<Qualification> Qualifications { get; set; }
    DbSet<Responsibility> Responsibilities { get; set; }
    DbSet<Industry> Industries { get; set; }
    DbSet<UserCompany> UserCompanies { get; set; }
    DbSet<Company> Companies { get; set; }

}

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; set; }
    DbSet<OutboxArchivedMessage> OutboxArchivedMessages { get; set; }
    DbSet<OutboxDeadLetter> OutboxDeadLetters { get; set; }
    Task<int> SaveChangesAsync(string userId, CancellationToken cancellationToken);

}

public interface IUnitOfWork
{ 
    Task<(int id, Guid uid)> GetNextValueFromSequenceAsync(Type entityType, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(string userId, CancellationToken cancellationToken);
}

public interface ITransactionDbContext
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken); 
    ChangeTracker ChangeTracker { get; }
    
    DatabaseFacade Database { get; }
}

public interface IJobBoardDbContext :
    IUnitOfWork,
    IJobBoardQueryDbContext;