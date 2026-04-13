using AdminAPI.Contracts.Services;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries;

public partial class CompanyQueryService(DaprClient client, UserContextService accessor, ILogger<CompanyQueryService> logger)
: ICompanyQueryService
{
    public async Task<ApiResponse<List<CompanyResponse>>> ListAsync(CancellationToken ct)
    {
        LogListingCompanies(logger);
        var authHeader = accessor.GetHeader("Authorization");

        var message = client.CreateInvokeMethodRequest(HttpMethod.Get, "company-api", "api/companies");
        message.Headers.Add("Authorization", authHeader?.Trim());
        var result = await DaprExtensions.Process(() => client.InvokeMethodAsync<List<CompanyResponse>>(message, ct));
        LogCompaniesListed(logger, result.Data?.Count ?? 0);
        return result;
    }

    [LoggerMessage(LogLevel.Information, "Listing companies from company-api")]
    static partial void LogListingCompanies(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Companies listed: {Count} found")]
    static partial void LogCompaniesListed(ILogger logger, int count);
}
