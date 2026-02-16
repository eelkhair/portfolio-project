using System.Reflection;
using FluentValidation;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Infrastructure.Decorators;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assemblies = new[] { typeof(BaseCommandHandler).Assembly }.Concat(additionalAssemblies).ToArray();
        services.AddValidatorsFromAssemblyContaining(typeof(IHandler<,>));
        services.AddScoped<IAiToolHandlerResolver, AiToolHandlerResolver>();
        
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.AddScoped<IHandlerContext, HandlerContext>();
        services.AddScoped<IConversationContext, ConversationContext>();
        services.Decorate(typeof(IHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(NormalizationCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(ObservabilityCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(UserContextCommandHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(ConversationContextDecorator<,>));
        services.AddMemoryCache();
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IAiPrompt<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo<IChatSystemPrompt>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );
        return services;
    }
}
