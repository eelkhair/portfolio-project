using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;

namespace AdminAPI.Contracts.Services;

public interface ICompanyQueryService
{
    Task<ApiResponse<List<CompanyResponse>>> ListAsync(CancellationToken ct);
}
