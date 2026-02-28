using System.Net.Http.Json;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Dapr.Client;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public class AdminApiClient(DaprClient client, IUserAccessor accessor, ILogger<AdminApiClient> logger) : BaseApiClient(client, accessor), IAdminApiClient
{
    public async Task<ApiResponse<List<CompanyResponse>>> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "companies", "admin-api");
            return await Client.InvokeMethodAsync<ApiResponse<List<CompanyResponse>>>(request, cancellationToken);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            
            logger.LogError(ex, "Error getting companies from admin-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(CreateCompanyRequest cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "companies", "admin-api");
            request.Content = JsonContent.Create(cmd);
            var response = await Client.InvokeMethodAsync<ApiResponse<CompanyResponse>>(request, ct);
            return response;
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error creating company in admin-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "jobs", "admin-api");
            request.Content = JsonContent.Create(cmd);
            return await Client.InvokeMethodAsync<ApiResponse<object>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error creating job in admin-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<List<IndustryResponse>>> ListIndustriesAsync(CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "industries", "admin-api");
            return await Client.InvokeMethodAsync<ApiResponse<List<IndustryResponse>>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting industries from monolith-api: {Body}", body);

            throw;
        }
    }
}