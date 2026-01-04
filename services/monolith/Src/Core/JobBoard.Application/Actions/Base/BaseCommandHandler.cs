using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


// ReSharper disable SuspiciousTypeConversion.Global

namespace JobBoard.Application.Actions.Base;

public abstract class BaseCommandHandler
{
    protected BaseCommandHandler(IHandlerContext handlerContext)
    {
        MetricsService = handlerContext.MetricsService;
        UnitOfWorkEvents = handlerContext.UnitOfWorkEvents;
        Context = handlerContext.UnitOfWork;
        Logger = handlerContext.LoggerFactory.CreateLogger(GetType());
        OutboxPublisher = handlerContext.OutboxPublisher;
        ((ITransactionDbContext) Context).ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }
    protected IUnitOfWork Context { get; }
    protected ILogger Logger { get; }
    protected IOutboxPublisher OutboxPublisher { get; }
    protected IMetricsService MetricsService { get; }
    protected IUnitOfWorkEvents UnitOfWorkEvents { get; }
    
}