using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Base;

public class HandlerContext(
    IUnitOfWork unitOfWork,
    IOutboxPublisher outboxPublisher,
    IMetricsService metricsService,
    IUnitOfWorkEvents unitOfWorkEvents, ILoggerFactory loggerFactory)
    : IHandlerContext
{
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;
    public IOutboxPublisher OutboxPublisher { get; } = outboxPublisher;
    public IMetricsService MetricsService { get; } = metricsService;
    public IUnitOfWorkEvents UnitOfWorkEvents { get; } = unitOfWorkEvents;
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;
}