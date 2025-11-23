using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries;

public class CompanyQueryService(DaprClient client, UserContextService accessor)
: ICompanyQueryService
{
    public async Task<ApiResponse<List<CompanyResponse>>> ListAsync(CancellationToken ct)
    {
        var authHeader= accessor.GetHeader("Authorization");

        var message = client.CreateInvokeMethodRequest(HttpMethod.Get, "company-api", "companies");
        message.Headers.Add("Authorization", authHeader?.Trim());
        return  await DaprExtensions.Process(()=> client.InvokeMethodAsync<List<CompanyResponse>>(message, ct));
    }
}