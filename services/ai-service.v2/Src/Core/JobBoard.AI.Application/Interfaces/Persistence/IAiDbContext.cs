using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.AI.Application.Interfaces.Persistence;

public interface IAiDbContext
{
    DbSet<Draft> Drafts { get; }
    DbSet<JobEmbedding> JobEmbeddings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    DbSet<T> Set<T>() where T : class;
}
