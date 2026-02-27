// ReSharper disable UnusedTypeParameter

using Microsoft.Extensions.Logging;

// ReSharper disable UnusedMemberInSuper.Global

namespace JobBoard.AI.Application.Interfaces.Configurations;

    public interface IHandler<in TRequest, TResult> where TRequest : IRequest<TResult>
    {
        Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public interface INoTransaction;
    public interface ISystemCommand;
    public interface IRequest<TResult>
    {
        public string UserId { get; set; }
    }
    
    public interface IHandlerContext
    {
        ILoggerFactory LoggerFactory { get; }
    }


    