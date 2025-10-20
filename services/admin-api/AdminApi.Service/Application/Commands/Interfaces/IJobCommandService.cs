using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands.Interfaces;

public interface IJobCommandService
{
    Task<ApiResponse<JobDraftResponse>> CreateDraft(string companyId, JobDraftRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<JobRewriteResponse>> RewriteItem(JobRewriteRequest request, CancellationToken ct);
}