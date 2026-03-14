using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobBoard.AI.Application.Interfaces.Clients;

public interface IAdminApiClient
{
    Task<ApiResponse<List<CompanyResponse>>> ListCompaniesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest cmd, CancellationToken ct);
    Task<ApiResponse<CompanyResponse>> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest cmd, CancellationToken ct);
    Task<ApiResponse<List<IndustryResponse>>> ListIndustriesAsync(CancellationToken ct);
    Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct);
    Task<ApiResponse<List<JobResponse>>> ListJobsAsync(Guid companyUId, CancellationToken ct);
    Task<ApiResponse<List<CompanyJobSummaryDto>>> ListCompanyJobSummariesAsync(CancellationToken ct);

    // Draft CRUD — routed via admin-api → job-api
    Task<ApiResponse<List<JobDraftResponse>>> ListDraftsAsync(Guid companyId, CancellationToken ct);
    Task<ApiResponse<JobDraftResponse>> SaveDraftAsync(Guid companyId, object draft, CancellationToken ct);
    Task DeleteDraftAsync(Guid companyId, Guid draftId, CancellationToken ct);
    Task<ApiResponse<JobDraftResponse?>> GetDraftByIdAsync(Guid draftId, CancellationToken ct);
}
