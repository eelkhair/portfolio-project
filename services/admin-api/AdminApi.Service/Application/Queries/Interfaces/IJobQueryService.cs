
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Queries.Interfaces;

public interface IJobQueryService
{
    Task<ApiResponse<List<JobResponse>>> ListAsync(Guid companyUId, CancellationToken ct);
    Task<ApiResponse<List<JobDraftResponse>>> ListDrafts(string companyId, CancellationToken ct = default);

}