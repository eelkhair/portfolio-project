using JobBoard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// ReSharper disable SuspiciousTypeConversion.Global

namespace JobBoard.Application.Actions.Base;

public abstract class BaseQueryHandler
{
    protected BaseQueryHandler(IJobBoardQueryDbContext context,
                                ILogger logger)
    {
        Context = context;
        Logger = logger;
        ((ITransactionDbContext)  Context).ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    } 
    protected IJobBoardQueryDbContext Context { get; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    protected ILogger Logger { get; } 
}
