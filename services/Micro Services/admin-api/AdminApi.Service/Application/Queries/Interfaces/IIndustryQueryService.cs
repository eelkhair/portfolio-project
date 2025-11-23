using CompanyAPI.Contracts.Models.Industries.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries.Interfaces;

public interface IIndustryQueryService
{
    Task<ApiResponse<List<IndustryResponse>>> ListAsync(CancellationToken ct);
}