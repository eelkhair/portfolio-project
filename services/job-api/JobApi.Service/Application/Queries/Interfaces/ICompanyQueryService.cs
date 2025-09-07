using JobAPI.Contracts.Models.Companies.Responses;

namespace JobApi.Application.Queries.Interfaces;

public interface ICompanyQueryService
{
    Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct);
}