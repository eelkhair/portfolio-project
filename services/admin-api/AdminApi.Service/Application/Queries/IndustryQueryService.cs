using AdminApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Dapr.Client;

namespace AdminApi.Application.Queries;

public class IndustryQueryService(DaprClient daprClient) : IIndustryQueryService
{
    public async Task<List<IndustryResponse>> ListAsync(CancellationToken ct)
    { 
        return await daprClient.InvokeMethodAsync<List<IndustryResponse>>(HttpMethod.Get, "company-api", "industries", ct);
    }
}