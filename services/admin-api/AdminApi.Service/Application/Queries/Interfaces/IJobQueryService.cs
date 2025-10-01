using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries.Interfaces;

public interface IJobQueryService
{
    Task<ApiResponse<List<JobResponse>>> ListAsync(Guid companyUId, CancellationToken ct);
}