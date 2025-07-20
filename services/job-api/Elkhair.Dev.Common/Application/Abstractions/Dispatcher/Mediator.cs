using Microsoft.Extensions.DependencyInjection;

namespace Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

public class Mediator
{
    private readonly IServiceProvider _provider;

    public Mediator(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        dynamic handler = _provider.GetRequiredService(handlerType);
        return handler.HandleAsync((dynamic)request, cancellationToken);
    }
}