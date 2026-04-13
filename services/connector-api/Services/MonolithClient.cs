using System.Diagnostics;
using System.Text.Json;
using ConnectorAPI.Helpers;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Services;

public class MonolithOClient(HttpClient httpClient, ActivitySource activitySource, ILogger<MonolithOClient> logger) : IMonolithClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<(CompanyCreateCompanyResult Company, CompanyCreateUserResult Admin)>
        GetCompanyAndAdminForCreatedEventAsync(
            Guid companyId,
            Guid adminId,
            string userId,
            CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("monolith.GetCompanyAndAdmin");
        activity?.SetTag("company.id", companyId.ToString());
        activity?.SetTag("admin.id", adminId.ToString());

        var companyRoute = ODataRouteBuilder.CompanyById(companyId, q =>
        {
            q["$select"] = "name,email,website,industryUId";
        });

        var adminRoute = ODataRouteBuilder.UserById(adminId, q =>
        {
            q["$select"] = "firstname,lastname,email,id";
        });

        logger.LogInformation("Fetching company {CompanyId} and admin {AdminId} from monolith via HTTP",
            companyId, adminId);

        var companyTask = GetAsync<CompanyCreateCompanyResult>(companyRoute, userId, cancellationToken);
        var adminTask = GetAsync<CompanyCreateUserResult>(adminRoute, userId, cancellationToken);

        await Task.WhenAll(companyTask, adminTask);

        return (await companyTask, await adminTask);
    }

    public async Task ActivateCompanyAsync(CompanyCreatedV1Event eventData, CompanyCreateCompanyResult company,
        CompanyCreatedUserApiPayload userApiResponse, CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("monolith.ActivateCompany");
        activity?.SetTag("company.uid", eventData.CompanyUId.ToString());

        var model = new ActivateCompanyRequest
        {
            KeycloakGroupId = userApiResponse.KeycloakGroupId,
            KeycloakUserId = userApiResponse.KeycloakUserId,
            CompanyEmail = company.Email,
            CompanyName = company.Name,
            CompanyUId = eventData.CompanyUId,
            CreatedBy = eventData.UserId,
            UserUId = eventData.AdminUId
        };

        logger.LogInformation("Activating company {CompanyUId} in the monolith via HTTP", eventData.CompanyUId);

        var response = await httpClient.PostAsJsonAsync("api/companies/company-created-success", model, JsonOpts, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CompanyUpdateCompanyResult> GetCompanyForUpdatedEventAsync(
        Guid companyId,
        string userId,
        CancellationToken cancellationToken)
    {
        using var activity = activitySource.StartActivity("monolith.GetCompanyForUpdate");
        activity?.SetTag("company.id", companyId.ToString());

        var companyRoute = ODataRouteBuilder.CompanyById(companyId, q =>
        {
            q["$select"] = "name,email,website,phone,description,about,eeo,founded,size,logo,industryUId";
        });

        logger.LogInformation("Fetching company {CompanyId} for update from monolith via HTTP", companyId);

        return await GetAsync<CompanyUpdateCompanyResult>(companyRoute, userId, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string route, string userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        if (!string.IsNullOrWhiteSpace(userId))
            request.Headers.TryAddWithoutValidation("x-user-id", userId);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>(JsonOpts, cancellationToken))!;
    }
}
