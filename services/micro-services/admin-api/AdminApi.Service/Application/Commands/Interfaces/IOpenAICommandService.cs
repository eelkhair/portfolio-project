using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands.Interfaces;

public interface IOpenAICommandService
{ 
    Task<ApiResponse<JobGenResponse>> GenerateJobAsync(string companyId, JobGenRequest request,
        CancellationToken ct = default);
}