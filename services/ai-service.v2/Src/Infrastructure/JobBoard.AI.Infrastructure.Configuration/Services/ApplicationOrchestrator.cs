using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class ApplicationOrchestrator(IServiceProvider serviceProvider) : IApplicationOrchestrator
{
    public async Task<TResult> ExecuteCommandAsync<TResult>(BaseCommand<TResult> command, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var handlerType = typeof(IHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        return await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<TResult> ExecuteQueryAsync<TResult>(BaseQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var handlerType = typeof(IHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        return await ((dynamic)handler).HandleAsync((dynamic)query, cancellationToken);
    }
}
