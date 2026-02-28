using ConnectorAPI.Helpers;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;

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

    public async Task ActivateCompanyAsync(CompanyCreatedV1Event eventData, CompanyCreateCompanyResult company,
        CompanyCreatedUserApiPayload userApiResponse, CancellationToken cancellationToken)
    {
        var model = new ActivateCompanyRequest
        {
            Auth0CompanyId = userApiResponse.Auth0OrganizationId,
            Auth0UserId = userApiResponse.Auth0UserId,
            CompanyEmail = company.Email,
            CompanyName = company.Name,
            CompanyUId = eventData.CompanyUId,
            CreatedBy = eventData.UserId,
            UserUId = eventData.AdminUId
        };
        logger.LogInformation("Activating company in the monolith api");
        var message = daprClient.CreateInvokeMethodRequest(HttpMethod.Post, MonolithAppId, "companies/company-created-success");
        message.Content= JsonContent.Create(model);
        await daprClient.InvokeMethodAsync(message, cancellationToken);
    }

    public async Task<CompanyUpdateCompanyResult> GetCompanyForUpdatedEventAsync(
        Guid companyId,
        string userId,
        CancellationToken cancellationToken)
    {
        var companyRoute = ODataRouteBuilder.CompanyById(companyId, q =>
        {
            q["$select"] = "name,email,website,phone,description,about,eeo,founded,size,logo,industryId";
        });

        var companyRequest = CreateGetRequest(companyRoute, userId);

        logger.LogDebug("Invoking monolith OData for company update: {CompanyRoute}", companyRoute);

        return await daprClient.InvokeMethodAsync<CompanyUpdateCompanyResult>(
            companyRequest, cancellationToken);
    }

    private HttpRequestMessage CreateGetRequest(string route, string userId)
    {
        var req = daprClient.CreateInvokeMethodRequest(HttpMethod.Get, MonolithAppId, route);

        if (!string.IsNullOrWhiteSpace(userId))
            req.Headers.TryAddWithoutValidation("x-user-id", userId);

        return req;
    }
}
