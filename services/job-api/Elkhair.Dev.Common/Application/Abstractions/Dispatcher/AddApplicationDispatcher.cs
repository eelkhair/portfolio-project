using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDispatcher(this IServiceCollection services, Assembly assembly)
    {
        services.AddSingleton<Mediator>();

        var handlerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
            .Where(t => t.Interface.IsGenericType &&
                        t.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.Interface, handler.Type);
        }

        return services;
    }
}