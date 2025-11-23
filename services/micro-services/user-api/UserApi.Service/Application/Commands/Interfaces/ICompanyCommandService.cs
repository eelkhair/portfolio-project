using System.Security.Claims;
using Auth0.ManagementApi.Models;
using UserAPI.Contracts.Models.Events;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Application.Commands.Interfaces;

public interface ICompanyCommandService
{
    Task<int> CreateUser(CreateUserRequest request, ClaimsPrincipal principal, CancellationToken ct);
    Task<int> CreateCompany(CreateCompanyRequest request, ClaimsPrincipal principal, CancellationToken ct);
    Task AddUserToCompany(int userId, int companyId, ClaimsPrincipal principal, CancellationToken ct);
}