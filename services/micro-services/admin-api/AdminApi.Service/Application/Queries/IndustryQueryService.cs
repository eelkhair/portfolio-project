using AdminApi.Application.Queries.Interfaces;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries;

public class IndustryQueryService(DaprClient daprClient, UserContextService accessor) : IIndustryQueryService
{
    public async Task<ApiResponse<List<IndustryResponse>>> ListAsync(CancellationToken ct)
    { 
        var authHeader= accessor.GetHeader("Authorization");

        var message = daprClient.CreateInvokeMethodRequest(HttpMethod.Get, "company-api", "industries");
        message.Headers.Add("Authorization", authHeader?.Trim());
        return await DaprExtensions.Process(()=> daprClient.InvokeMethodAsync<List<IndustryResponse>>(message, ct));
    }
}