using AdminAPI.Contracts.Services;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;

namespace AdminApi.Application.Queries;

public partial class IndustryQueryService(DaprClient daprClient, UserContextService accessor, ILogger<IndustryQueryService> logger) : IIndustryQueryService
{
    public async Task<ApiResponse<List<IndustryResponse>>> ListAsync(CancellationToken ct)
    {
        LogListingIndustries(logger);
        var authHeader= accessor.GetHeader("Authorization");

        var message = daprClient.CreateInvokeMethodRequest(HttpMethod.Get, "company-api", "api/industries");
        message.Headers.Add("Authorization", authHeader?.Trim());
        var result = await DaprExtensions.Process(()=> daprClient.InvokeMethodAsync<List<IndustryResponse>>(message, ct));
        LogIndustriesListed(logger, result.Data?.Count ?? 0);
        return result;
    }

    [LoggerMessage(LogLevel.Information, "Listing industries from company-api")]
    static partial void LogListingIndustries(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Industries listed: {Count} found")]
    static partial void LogIndustriesListed(ILogger logger, int count);
}
