using CompanyAPI.Contracts.Models.Companies.Responses;

namespace CompanyApi.Application.Queries.Interfaces;

public interface ICompanyQueryService
{
    Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct);
}