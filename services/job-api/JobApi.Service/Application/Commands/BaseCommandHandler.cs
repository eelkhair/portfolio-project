using JobApi.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application.Commands;

public abstract class BaseCommandHandler
{
    protected BaseCommandHandler(IJobDbContext context)
    {
        Context = context;
        Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    protected readonly IJobDbContext Context;
    
}