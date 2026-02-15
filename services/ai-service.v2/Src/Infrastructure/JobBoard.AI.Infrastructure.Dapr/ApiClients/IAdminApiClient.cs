using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public interface IAdminApiClient
{
    Task<ApiResponse<List<CompanyResponse>>> ListCompaniesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest cmd, CancellationToken ct);
    Task<ApiResponse<List<IndustryResponse>>> ListIndustriesAsync(CancellationToken ct);

}