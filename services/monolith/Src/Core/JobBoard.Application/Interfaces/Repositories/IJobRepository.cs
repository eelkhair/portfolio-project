using JobBoard.Domain.Entities;

namespace JobBoard.Application.Interfaces.Repositories;

public interface IJobRepository : IRepository
{
    Task AddAsync(Job job, CancellationToken cancellationToken);
}