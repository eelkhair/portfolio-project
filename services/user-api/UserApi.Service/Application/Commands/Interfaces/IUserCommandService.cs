using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands.Interfaces;

public interface IUserCommandService
{
    Task ProvisionUserAsync(ProvisionUserEvent user, CancellationToken ct);
}