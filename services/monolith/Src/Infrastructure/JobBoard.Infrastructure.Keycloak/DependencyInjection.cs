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
}
