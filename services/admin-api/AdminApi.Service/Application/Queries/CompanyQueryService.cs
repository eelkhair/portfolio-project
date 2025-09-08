using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;

namespace AdminApi.Application.Queries;

public class CompanyQueryService(DaprClient client)
: ICompanyQueryService
{
    public async Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct)
    {
        return await client.InvokeMethodAsync<List<CompanyResponse>>(HttpMethod.Get, "company-api", "companies", ct);
    }
}