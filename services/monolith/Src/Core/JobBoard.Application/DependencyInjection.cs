using FluentValidation;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Decorators;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Infrastructure.UserSync;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<BaseCommandHandler>()
            .AddClasses(c => c.AssignableTo(typeof(IHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        
        services.AddValidatorsFromAssemblyContaining(typeof(IHandler<,>));

        services.Decorate(typeof(IHandler<,>), typeof(ExceptionHandlingCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(TransactionCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(ObservabilityCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(UserContextCommandHandlerDecorator<,>));

        services.AddTransient<IUserSyncService, UserSyncService>();
        services.AddScoped<IHandlerContext, HandlerContext>();
        return services;
    }
}