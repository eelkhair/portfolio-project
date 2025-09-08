using AdminAPI.Contracts.Models.Companies.Responses;

namespace AdminApi.Application.Queries.Interfaces;

public interface ICompanyQueryService
{
    Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct);
}