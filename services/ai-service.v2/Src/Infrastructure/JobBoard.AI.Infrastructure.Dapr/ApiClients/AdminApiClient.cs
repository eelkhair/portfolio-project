using System.Net.Http.Json;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public class AdminApiClient(DaprClient client, IUserAccessor accessor, ILogger<AdminApiClient> logger) : BaseApiClient(client, accessor), IAdminApiClient
{
    public async Task<ApiResponse<List<CompanyResponse>>> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "api/companies", "admin-api");
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
            var request = CreateRequest(HttpMethod.Post, "api/companies", "admin-api");
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

    public async Task<ApiResponse<CompanyResponse>> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Put, $"api/companies/{companyId}", "admin-api");
            request.Content = JsonContent.Create(cmd);
            var response = await Client.InvokeMethodAsync<ApiResponse<CompanyResponse>>(request, ct);
            return response;
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error updating company in admin-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/jobs", "admin-api");
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

    public async Task<ApiResponse<List<JobResponse>>> ListJobsAsync(Guid companyUId, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"api/jobs/{companyUId}", "admin-api");
            return await Client.InvokeMethodAsync<ApiResponse<List<JobResponse>>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting jobs from admin-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<List<CompanyJobSummaryDto>>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "api/companies/job-summaries", "admin-api");
            return await Client.InvokeMethodAsync<ApiResponse<List<CompanyJobSummaryDto>>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting company job summaries from admin-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<List<IndustryResponse>>> ListIndustriesAsync(CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "api/industries", "admin-api");
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