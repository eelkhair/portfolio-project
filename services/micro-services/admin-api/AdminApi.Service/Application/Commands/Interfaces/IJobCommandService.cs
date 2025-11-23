using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Commands.Interfaces;

public interface IJobCommandService
{
    Task<ApiResponse<JobDraftResponse>> CreateDraft(string companyId, JobDraftRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<JobRewriteResponse>> RewriteItem(JobRewriteRequest request, CancellationToken ct);
    Task<ApiResponse<JobResponse>> CreateJob(JobCreateRequest request, CancellationToken ct);
    Task<ApiResponse<bool>> DeleteDraft(string draftId, string companyId, CancellationToken ct);
}