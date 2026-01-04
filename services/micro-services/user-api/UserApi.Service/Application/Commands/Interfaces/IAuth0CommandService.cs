using Auth0.ManagementApi.Models;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands.Interfaces;

public interface IAuth0CommandService
{
    Task<(User, Organization)> ProvisionUserAsync(ProvisionUserEvent user, CancellationToken ct);
}