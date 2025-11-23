using System.Security.Claims;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Application.Interfaces;

public interface IJobCommandService
{
    Task<JobResponse> CreateJobAsync(CreateJobRequest request, ClaimsPrincipal user, CancellationToken ct);
}