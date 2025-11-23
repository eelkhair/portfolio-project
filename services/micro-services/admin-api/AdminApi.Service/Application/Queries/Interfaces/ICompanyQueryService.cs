using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries.Interfaces;

public interface ICompanyQueryService
{
    Task<ApiResponse<List<CompanyResponse>>> ListAsync(CancellationToken ct);
}