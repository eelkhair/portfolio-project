using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence.Repositories;

public class JobRepository(IJobBoardQueryDbContext context) : BaseRepository(context), IJobRepository
{
    public async Task AddAsync(Job job, CancellationToken cancellationToken)
    {
        await Context.Jobs.AddAsync(job, cancellationToken);
    }
    
    
}