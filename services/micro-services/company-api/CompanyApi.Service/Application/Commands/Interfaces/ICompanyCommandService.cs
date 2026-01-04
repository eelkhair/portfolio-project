using System.Security.Claims;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;

namespace CompanyApi.Application.Commands.Interfaces;

public interface ICompanyCommandService
{
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct);
    Task<bool> ActivateAsync(Guid companyUId, ClaimsPrincipal user, CancellationToken ct);
}