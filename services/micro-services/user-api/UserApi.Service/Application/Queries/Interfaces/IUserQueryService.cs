using UserAPI.Contracts.Models.Responses;

namespace UserApi.Application.Queries.Interfaces;

public interface IUserQueryService
{
    Task<List<UserResponse>> ListAsync(CancellationToken ct);
}
