using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elkhair.Dev.Common.Application;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Companies;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.HttpClients;

public class HttpMonolithApiClient(
    HttpClient client,
    ILogger<HttpMonolithApiClient> logger) : IMonolithApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<ODataResponse<List<CompanyDto>>> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await client.GetFromJsonAsync<ODataResponse<List<CompanyDto>>>("odata/companies", JsonOpts, cancellationToken)
                   ?? new ODataResponse<List<CompanyDto>>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error getting companies from monolith-api");
            throw;
        }
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyCommand cmd, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/companies", cmd, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOpts, ct);
            return result!.Data!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating company in monolith-api");
            throw;
        }
    }

    public async Task<CompanyDto> UpdateCompanyAsync(Guid companyId, UpdateCompanyCommand cmd, CancellationToken ct)
    {
        try
        {
            var response = await client.PutAsJsonAsync($"api/companies/{companyId}", cmd, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOpts, ct);
            return result!.Data!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error updating company in monolith-api");
            throw;
        }
    }

    public async Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/jobs", cmd, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOpts, ct)
                   ?? new ApiResponse<object>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating job in monolith-api");
            throw;
        }
    }

    public async Task<List<JobResponse>> ListJobsAsync(Guid companyUId, CancellationToken ct)
    {
        try
        {
            return await client.GetFromJsonAsync<List<JobResponse>>($"api/jobs/{companyUId}", JsonOpts, ct) ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error getting jobs from monolith-api");
            throw;
        }
    }

    public async Task<List<CompanyJobSummaryDto>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
        try
        {
            return await client.GetFromJsonAsync<List<CompanyJobSummaryDto>>("api/companies/job-summaries", JsonOpts, ct) ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error getting company job summaries from monolith-api");
            throw;
        }
    }

    public async Task<ODataResponse<List<IndustryDto>>> ListIndustriesAsync(CancellationToken ct)
    {
        try
        {
            return await client.GetFromJsonAsync<ODataResponse<List<IndustryDto>>>("odata/industries", JsonOpts, ct)
                   ?? new ODataResponse<List<IndustryDto>>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error getting industries from monolith-api");
            throw;
        }
    }

    public async Task NotifyResumeParseCompletedAsync(ResumeParseCompletedRequest model, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/resumes/parse-completed", model, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error notifying monolith of resume parse completion");
            throw;
        }
    }

    public async Task NotifyResumeParseFailedAsync(ResumeParseFailedRequest model, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/resumes/parse-failed", model, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error notifying monolith of resume parse failure");
            throw;
        }
    }

    public async Task NotifyResumeEmbeddedAsync(ResumeEmbeddedRequest model, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/resumes/embedded", model, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error notifying monolith of resume embedding completion");
            throw;
        }
    }

    public async Task<ResumeParsedContentResponse?> GetResumeParsedContentAsync(Guid resumeUId, CancellationToken ct)
    {
        try
        {
            var result = await client.GetFromJsonAsync<ApiResponse<ResumeParsedContentResponse>>(
                $"api/resumes/{resumeUId}/parsed-content/internal", JsonOpts, ct);
            return result?.Data;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error getting resume parsed content from monolith-api");
            throw;
        }
    }

    public async Task NotifySectionParsedAsync(ResumeSectionParsedRequest model, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/resumes/section-parsed", model, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error notifying monolith of section parse for {Section}", model.Section);
            throw;
        }
    }

    public async Task NotifySectionFailedAsync(ResumeSectionFailedRequest model, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/resumes/section-failed", model, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error notifying monolith of section failure for {Section}", model.Section);
            throw;
        }
    }

    public async Task NotifyAllSectionsCompletedAsync(ResumeAllSectionsCompletedRequest model, CancellationToken ct)
    {
        try
        {
            var response = await client.PostAsJsonAsync("api/resumes/all-sections-completed", model, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error notifying monolith of all sections completed");
            throw;
        }
    }

    public async Task<List<DraftResponse>> ListDraftsAsync(Guid companyId, CancellationToken ct)
    {
        try
        {
            var result = await client.GetFromJsonAsync<ApiResponse<List<DraftResponse>>>($"api/jobs/{companyId}/list-drafts", JsonOpts, ct);
            return result?.Data ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error listing drafts from monolith-api");
            throw;
        }
    }

    public async Task<DraftResponse> SaveDraftAsync(Guid companyId, DraftResponse draft, CancellationToken ct)
    {
        try
        {
            var response = await client.PutAsJsonAsync($"api/jobs/{companyId}/save-draft", draft, JsonOpts, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DraftResponse>>(JsonOpts, ct);
            return result!.Data!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error saving draft in monolith-api");
            throw;
        }
    }

    public async Task DeleteDraftAsync(Guid companyId, Guid draftId, CancellationToken ct)
    {
        try
        {
            var response = await client.DeleteAsync($"api/jobs/{companyId}/drafts/{draftId}", ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error deleting draft from monolith-api");
            throw;
        }
    }

    public async Task<DraftResponse?> GetDraftByIdAsync(Guid draftId, CancellationToken ct)
    {
        try
        {
            var result = await client.GetFromJsonAsync<ApiResponse<DraftResponse>>($"api/jobs/drafts/{draftId}", JsonOpts, ct);
            return result?.Data;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error getting draft from monolith-api");
            throw;
        }
    }

    public async Task<Dictionary<Guid, DraftsByCompanyItemResponse>> ListAllDraftsByCompanyAsync(CancellationToken ct)
    {
        try
        {
            var result = await client.GetFromJsonAsync<ApiResponse<Dictionary<Guid, DraftsByCompanyItemResponse>>>("api/jobs/drafts/by-company", JsonOpts, ct);
            return result?.Data ?? new();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error listing drafts by company from monolith-api");
            throw;
        }
    }
}
