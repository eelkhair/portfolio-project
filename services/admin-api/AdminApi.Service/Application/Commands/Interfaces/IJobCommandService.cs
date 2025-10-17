using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands.Interfaces;

public interface IJobCommandService
{
    Task<ApiResponse<JobDraftResponse>> CreateDraft(string companyId, JobDraftRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<List<JobDraftResponse>>> ListDrafts(string companyId, CancellationToken ct = default);
}