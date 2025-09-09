using System.Security.Claims;
using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Companies.Responses;

namespace AdminApi.Application.Commands.Interfaces;

public interface ICompanyCommandService
{
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken ct);
    
}