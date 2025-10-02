using AdminApi.Application.Queries.Interfaces;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Queries;

public class JobQueryService(DaprClient client, UserContextService accessor) : IJobQueryService
{
    public async Task<ApiResponse<List<JobResponse>>> ListAsync(Guid companyUId, CancellationToken ct)
    {
        var authHeader= accessor.GetHeader("Authorization");

        var request = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", $"jobs/{companyUId}");
        request.Headers.Add("Authorization", authHeader?.Trim());
        return await DaprExtensions.Process(()=> client.InvokeMethodAsync<List<JobResponse>>(request, ct));

    }
}