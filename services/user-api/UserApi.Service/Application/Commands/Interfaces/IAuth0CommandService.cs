using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands.Interfaces;

public interface IAuth0CommandService
{
    Task<bool> ProvisionUserAsync(ProvisionUserEvent user, CancellationToken ct);
}