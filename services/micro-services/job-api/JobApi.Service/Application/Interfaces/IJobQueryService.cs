using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Application.Interfaces;

public interface IJobQueryService
{
    Task<List<JobResponse>> ListAsync(Guid companyUId, CancellationToken ct);
    Task<List<CompanyJobSummaryResponse>> ListCompanyJobSummariesAsync(CancellationToken ct);
}

public record CompanyJobSummaryResponse(Guid CompanyId, string CompanyName, int JobCount);