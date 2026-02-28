using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public interface IAdminApiClient
{
    Task<ApiResponse<List<CompanyResponse>>> ListCompaniesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest cmd, CancellationToken ct);
    Task<ApiResponse<List<IndustryResponse>>> ListIndustriesAsync(CancellationToken ct);
    Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct);
    Task<ApiResponse<List<JobResponse>>> ListJobsAsync(Guid companyUId, CancellationToken ct);
}