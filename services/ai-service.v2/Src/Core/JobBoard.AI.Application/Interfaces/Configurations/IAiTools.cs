using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IAiTools
{
    IEnumerable<AITool> GetTools();
}

public sealed record ToolResultEnvelope<T>(
    T Data,
    int Count,
    DateTimeOffset ExecutedAt);
    
    
public interface IAiToolHandlerResolver
{
    IHandler<TRequest, TResponse> Resolve<TRequest, TResponse>()
        where TRequest : notnull, IRequest<TResponse>;
}

public sealed class AiToolHandlerResolver(
    IServiceProvider serviceProvider)
    : IAiToolHandlerResolver
{
    public IHandler<TRequest, TResponse> Resolve<TRequest, TResponse>()
        where TRequest : notnull, IRequest<TResponse> => serviceProvider.GetRequiredService<IHandler<TRequest, TResponse>>();
}