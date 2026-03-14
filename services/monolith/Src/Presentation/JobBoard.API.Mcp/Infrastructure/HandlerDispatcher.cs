using JobBoard.Application.Interfaces.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.API.Mcp.Infrastructure;

/// <summary>
/// Dispatches commands and queries through the full CQRS decorator pipeline
/// (UserContext → Observability → Validation → Transaction → ExceptionHandling → Handler).
/// Equivalent to BaseApiController.ExecuteCoreAsync but usable from MCP tools.
/// </summary>
public class HandlerDispatcher(IServiceProvider serviceProvider)
{
    public Task<TResult> DispatchAsync<TRequest, TResult>(TRequest request, CancellationToken ct)
        where TRequest : IRequest<TResult>
    {
        var handlerType = typeof(IHandler<,>).MakeGenericType(typeof(TRequest), typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);
        return ((dynamic)handler).HandleAsync((dynamic)request, ct);
    }
}
