using ConnectorAPI.Helpers;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Models;
using Dapr.Client;

namespace ConnectorAPI.Services;

public class MonolithOClient(DaprClient daprClient, ILogger<MonolithOClient> logger) : IMonolithClient
{
    private const string MonolithAppId = "monolith-api";

    public async Task<(CompanyCreateCompanyResult Company, CompanyCreateUserResult Admin)>
        GetCompanyAndAdminForCreatedEventAsync(
            Guid companyId,
            Guid adminId,
            string userId,
            CancellationToken cancellationToken)
    {
        var companyRoute = ODataRouteBuilder.CompanyById(companyId, q =>
        {
            q["$select"] = "name,email,website,industryId";
        });

        var adminRoute = ODataRouteBuilder.UserById(adminId, q =>
        {
            q["$select"] = "firstname,lastname,email,id";
        });

        var companyRequest = CreateGetRequest(companyRoute, userId);
        var adminRequest   = CreateGetRequest(adminRoute,   userId);

        logger.LogDebug("Invoking monolith OData: {CompanyRoute} & {AdminRoute}",
            companyRoute, adminRoute);

        var companyTask = daprClient.InvokeMethodAsync<CompanyCreateCompanyResult>(
            companyRequest, cancellationToken);

        var adminTask = daprClient.InvokeMethodAsync<CompanyCreateUserResult>(
            adminRequest, cancellationToken);

        await Task.WhenAll(companyTask, adminTask);

        var company = await companyTask;
        var admin   = await adminTask;

        return (company, admin);
    }

    private HttpRequestMessage CreateGetRequest(string route, string userId)
    {
        var req = daprClient.CreateInvokeMethodRequest(HttpMethod.Get, MonolithAppId, route);

        if (!string.IsNullOrWhiteSpace(userId))
            req.Headers.TryAddWithoutValidation("x-user-id", userId);

        return req;
    }
}
