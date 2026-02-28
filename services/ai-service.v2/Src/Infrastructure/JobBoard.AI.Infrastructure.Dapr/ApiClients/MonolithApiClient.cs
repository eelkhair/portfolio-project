using System.Net.Http.Json;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;
using JobBoard.Monolith.Contracts.Companies;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobBoard.AI.Infrastructure.Dapr.AITools.Shared;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public class MonolithApiClient(DaprClient _, IUserAccessor accessor, ILogger<MonolithApiClient> logger) 
    : BaseApiClient(_, accessor), IMonolithApiClient
{
    public async Task<ODataResponse<List<CompanyDto>>> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "odata/companies", "monolith-api");
            return await Client.InvokeMethodAsync<ODataResponse<List<CompanyDto>>>(request, cancellationToken);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            
            logger.LogError(ex, "Error getting companies from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyCommand cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "companies", "monolith-api");
            request.Content = JsonContent.Create(cmd);
            var response = await Client.InvokeMethodAsync<ApiResponse<CompanyDto>>(request, ct);
            return response.Data!;
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error creating company in monolith-api: {Body}", body);
            throw;
        }
    }
    

    public async Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "jobs", "monolith-api");
            request.Content = JsonContent.Create(cmd);
            return await Client.InvokeMethodAsync<ApiResponse<object>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error creating job in monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<List<JobResponse>> ListJobsAsync(Guid companyUId, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"jobs/{companyUId}", "monolith-api");
            return await Client.InvokeMethodAsync<List<JobResponse>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting jobs from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<List<CompanyJobSummaryDto>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "companies/job-summaries", "monolith-api");
            return await Client.InvokeMethodAsync<List<CompanyJobSummaryDto>>(request, ct);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting company job summaries from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<ODataResponse<List<IndustryDto>>> ListIndustriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "odata/industries", "monolith-api");
            return await Client.InvokeMethodAsync<ODataResponse<List<IndustryDto>>>(request, cancellationToken);
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(ex, "Error getting industries from monolith-api: {Body}", body);

            throw;
        }
    }
}

public  class ApiResponse<T>
{
    public ApiError? Exceptions { get; set; }
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string StatusCode { get; set; } = default!;
}