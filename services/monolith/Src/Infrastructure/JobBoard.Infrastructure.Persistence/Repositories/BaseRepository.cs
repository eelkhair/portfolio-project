using JobBoard.Application.Interfaces;

namespace JobBoard.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository(IJobBoardQueryDbContext context)
{
    protected readonly IJobBoardQueryDbContext Context = context;
}