using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands.Interfaces;

public interface ICompanyCommandService
{
    Task<ApiResponse<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken ct);
    Task<ApiResponse<CompanyResponse>> UpdateAsync(Guid companyUId, UpdateCompanyRequest request, CancellationToken ct);
}