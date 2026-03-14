using System.Net.Http.Json;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Companies;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public class MonolithApiClient(DaprClient _, IUserAccessor accessor, ILogger<MonolithApiClient> logger)
    : BaseApiClient(_, accessor, logger), IMonolithApiClient
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
            var request = CreateRequest(HttpMethod.Post, "api/companies", "monolith-api");
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


    public async Task<CompanyDto> UpdateCompanyAsync(Guid companyId, UpdateCompanyCommand cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Put, $"api/companies/{companyId}", "monolith-api");
            request.Content = JsonContent.Create(cmd);
            var response = await Client.InvokeMethodAsync<ApiResponse<CompanyDto>>(request, ct);
            return response.Data!;
        }
        catch (InvocationException ex)
        {
            var response = ex.Response;
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error updating company in monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/jobs", "monolith-api");
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
            var request = CreateRequest(HttpMethod.Get, $"api/jobs/{companyUId}", "monolith-api");
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
            var request = CreateRequest(HttpMethod.Get, "api/companies/job-summaries", "monolith-api");
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

    public async Task NotifyResumeParseCompletedAsync(ResumeParseCompletedRequest model, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/resumes/parse-completed", "monolith-api");
            request.Content = JsonContent.Create(model);
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error notifying monolith of resume parse completion: {Body}", body);
            throw;
        }
    }

    public async Task NotifyResumeParseFailedAsync(ResumeParseFailedRequest model, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/resumes/parse-failed", "monolith-api");
            request.Content = JsonContent.Create(model);
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error notifying monolith of resume parse failure: {Body}", body);
            throw;
        }
    }

    public async Task NotifyResumeEmbeddedAsync(ResumeEmbeddedRequest model, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/resumes/embedded", "monolith-api");
            request.Content = JsonContent.Create(model);
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error notifying monolith of resume embedding completion: {Body}", body);
            throw;
        }
    }

    public async Task<ResumeParsedContentResponse?> GetResumeParsedContentAsync(Guid resumeUId, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"api/resumes/{resumeUId}/parsed-content/internal", "monolith-api");
            var response = await Client.InvokeMethodAsync<ApiResponse<ResumeParsedContentResponse>>(request, ct);
            return response.Data;
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting resume parsed content from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task NotifySectionParsedAsync(ResumeSectionParsedRequest model, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/resumes/section-parsed", "monolith-api");
            request.Content = JsonContent.Create(model);
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error notifying monolith of section parse for {Section}: {Body}", model.Section, body);
            throw;
        }
    }

    public async Task NotifySectionFailedAsync(ResumeSectionFailedRequest model, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/resumes/section-failed", "monolith-api");
            request.Content = JsonContent.Create(model);
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error notifying monolith of section failure for {Section}: {Body}", model.Section, body);
            throw;
        }
    }

    public async Task NotifyAllSectionsCompletedAsync(ResumeAllSectionsCompletedRequest model, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "api/resumes/all-sections-completed", "monolith-api");
            request.Content = JsonContent.Create(model);
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error notifying monolith of all sections completed: {Body}", body);
            throw;
        }
    }

    public async Task<List<DraftResponse>> ListDraftsAsync(Guid companyId, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"api/jobs/{companyId}/list-drafts", "monolith-api");
            var response = await Client.InvokeMethodAsync<ApiResponse<List<DraftResponse>>>(request, ct);
            return response.Data ?? [];
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error listing drafts from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<DraftResponse> SaveDraftAsync(Guid companyId, DraftResponse draft, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Put, $"api/jobs/{companyId}/save-draft", "monolith-api");
            request.Content = JsonContent.Create(draft);
            var response = await Client.InvokeMethodAsync<ApiResponse<DraftResponse>>(request, ct);
            return response.Data!;
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error saving draft in monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task DeleteDraftAsync(Guid companyId, Guid draftId, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Delete, $"api/jobs/{companyId}/drafts/{draftId}", "monolith-api");
            await Client.InvokeMethodAsync(request, ct);
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error deleting draft from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<DraftResponse?> GetDraftByIdAsync(Guid draftId, CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"api/jobs/drafts/{draftId}", "monolith-api");
            var response = await Client.InvokeMethodAsync<ApiResponse<DraftResponse>>(request, ct);
            return response.Data;
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error getting draft from monolith-api: {Body}", body);
            throw;
        }
    }

    public async Task<Dictionary<Guid, DraftsByCompanyItemResponse>> ListAllDraftsByCompanyAsync(CancellationToken ct)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "api/jobs/drafts/by-company", "monolith-api");
            var response = await Client.InvokeMethodAsync<ApiResponse<Dictionary<Guid, DraftsByCompanyItemResponse>>>(request, ct);
            return response.Data ?? new();
        }
        catch (InvocationException ex)
        {
            var body = await ex.Response.Content.ReadAsStringAsync(ct);
            logger.LogError(ex, "Error listing drafts by company from monolith-api: {Body}", body);
            throw;
        }
    }
}
