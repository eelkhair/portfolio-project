using JobBoard.Application.Interfaces.Infrastructure.Keycloak;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.Keycloak;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Keycloak Admin API client services for the monolith. Reads config keys:
    /// Keycloak:Authority, Keycloak:TokenUrl, Keycloak:ServiceClientId, Keycloak:ServiceClientSecret.
    /// </summary>
    public static IServiceCollection AddKeycloakAdminClient(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient("keycloak-admin");
        services.AddSingleton<IKeycloakTokenProvider, KeycloakTokenProvider>();
        services.AddScoped<IKeycloakAdminClient, KeycloakAdminClient>();
        return services;
    }

    /// <summary>
    /// Registers the background service that periodically deletes expired anonymous (guest)
    /// Keycloak users. Safe to register on services that run multiple replicas — the sweep is
    /// idempotent (delete 404 is a no-op) and wasted work is bounded by the 6h interval.
    /// </summary>
    public static IServiceCollection AddAnonymousUserCleanup(this IServiceCollection services)
    {
        services.AddHostedService<AnonymousUserCleanupService>();
        return services;
    }
}
