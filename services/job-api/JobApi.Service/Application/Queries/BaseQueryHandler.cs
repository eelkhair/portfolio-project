using JobApi.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application.Queries;

public abstract class BaseQueryHandler
{
    protected BaseQueryHandler(IJobDbContext context)
    {
        Context = context;
        Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected readonly IJobDbContext Context;
    
}