using JobBoard.Application.Interfaces.Infrastructure.Turnstile;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.Turnstile;

public static class DependencyInjection
{
    public static IServiceCollection AddTurnstileVerifier(this IServiceCollection services)
    {
        services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();
        return services;
    }
}
