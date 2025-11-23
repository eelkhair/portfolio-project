using Auth0.ManagementApi.Models;
using Elkhair.Dev.Common.Application;

namespace UserApi.Infrastructure.Auth0.Interfaces;

/// <summary>
/// Thin wrapper over Auth0 ManagementApiClient with the operations you need.
/// Add more methods as your app grows.
/// </summary>
public interface IAuth0Resource
{
    Task<User> GetUserAsync(string userId, CancellationToken ct = default);
    Task<ApiResponse<Organization>> CreateOrganizationAsync(Guid uid, string name, CancellationToken ct);
    Task<ApiResponse<bool>> InviteUserAsync(string organizationId, string email, CancellationToken ct);
    Task<ApiResponse<User>> CreateUserAsync(User user, CancellationToken ct);
    Task<ApiResponse<bool>> AddMemberToOrganizationAsync(string organizationId, string userId, string role, CancellationToken ct);
}