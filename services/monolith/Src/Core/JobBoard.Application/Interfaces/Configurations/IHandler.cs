// ReSharper disable UnusedTypeParameter

using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedMemberInSuper.Global

namespace JobBoard.Application.Interfaces.Configurations;

    public interface IHandler<in TRequest, TResult> where TRequest : IRequest<TResult>
    {
        Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public interface INoTransaction;
    public interface IAnonymousRequest;
    public interface IRequest<TResult>
    {
        public string UserId { get; set; }
    }
    
    public interface IHandlerContext
    {
        IUnitOfWork UnitOfWork { get; }
        IOutboxPublisher OutboxPublisher { get; }
        IMetricsService MetricsService { get; }
        IUnitOfWorkEvents UnitOfWorkEvents { get; }
        ILoggerFactory LoggerFactory { get; }
    }


    