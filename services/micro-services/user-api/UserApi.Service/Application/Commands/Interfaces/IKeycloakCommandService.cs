using UserApi.Infrastructure.Keycloak;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands.Interfaces;

public interface IKeycloakCommandService
{
    Task<(KeycloakUser User, KeycloakGroup Group)> ProvisionUserAsync(ProvisionUserEvent user, CancellationToken ct);
}
