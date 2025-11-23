using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Application.Interfaces;

public interface IJobQueryService
{
    Task<List<JobResponse>> ListAsync(Guid companyUId, CancellationToken ct);
}