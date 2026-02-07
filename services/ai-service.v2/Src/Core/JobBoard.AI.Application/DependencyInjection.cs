using System.Reflection;
using FluentValidation;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assemblies = new[] { typeof(BaseCommandHandler).Assembly }.Concat(additionalAssemblies).ToArray();
        services.AddValidatorsFromAssemblyContaining(typeof(IHandler<,>));

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.AddScoped<IHandlerContext, HandlerContext>();

        return services;
    }
}
