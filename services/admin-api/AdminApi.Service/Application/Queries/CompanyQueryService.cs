using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;

namespace AdminApi.Application.Queries;

public class CompanyQueryService(ILogger<CompanyQueryService> logger)
: ICompanyQueryService
{
    public async Task<List<CompanyResponse>> ListAsync(HttpContext context, CancellationToken ct)
    {
        logger.LogInformation("hello");
       
        var client = new DaprClientBuilder().Build();
        return await client.InvokeMethodAsync<List<CompanyResponse>>(HttpMethod.Get, "job-api", "companies", ct);
        
    }
}