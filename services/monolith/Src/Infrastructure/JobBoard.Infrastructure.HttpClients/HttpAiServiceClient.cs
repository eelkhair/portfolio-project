using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elkhair.Dev.Common.Application;
using JobBoard.Application.Actions.Public;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.HttpClients;

public sealed class HttpAiServiceClient(
    HttpClient client,
    IActivityFactory activityFactory,
    ILogger<HttpAiServiceClient> logger)
    : IAiServiceClient
{
    private const string ServiceName = "ai-service-v2";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DraftRewriteResponse> RewriteItem(DraftItemRewriteRequest requestModel, CancellationToken cancellationToken)
    {
        EnrichActivity(null, "drafts.rewrite.item");
        using var response = await client.PutAsJsonAsync("drafts/rewrite/item", requestModel, JsonOpts, cancellationToken);
        await EnsureSuccess(response, "drafts.rewrite.item", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DraftRewriteResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");
        return result.Data!;
    }

    public async Task<DraftGenResponse> GenerateDraft(Guid companyId, DraftGenRequest requestModel, CancellationToken cancellationToken)
    {
        EnrichActivity(companyId, "drafts.generate");
        using var response = await client.PostAsJsonAsync($"drafts/{companyId}/generate", requestModel, JsonOpts, cancellationToken);
        await EnsureSuccess(response, "drafts.generate", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DraftGenResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        Activity.Current?.SetTag("ai.draft.id", result.Data?.DraftId);
        Activity.Current?.SetTag("ai.draft.title", result.Data?.Title);
        logger.LogInformation("{Service} generated draft {DraftId} with title '{Title}' for company {CompanyId}",
            ServiceName, result.Data?.DraftId, result.Data?.Title, companyId);
        return result.Data!;
    }

    public async Task<ProviderSettings> GetProvider(CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.get-provider");
        using var response = await client.GetAsync("settings/provider", cancellationToken);
        await EnsureSuccess(response, "settings.get-provider", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProviderSettings>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        logger.LogInformation("{Service} returned provider {Provider} with model {Model}", ServiceName, result.Data?.Provider, result.Data?.Model);
        return result.Data!;
    }

    public async Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.update-provider");
        using var response = await client.PutAsJsonAsync("settings/update-provider", request, JsonOpts, cancellationToken);
        await EnsureSuccess(response, "settings.update-provider", cancellationToken);

        logger.LogInformation("{Service} updated provider to {Provider} with model {Model}", ServiceName, request.Provider, request.Model);
    }

    public async Task<ApplicationModeDto> GetApplicationMode(CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.mode");
        using var response = await client.GetAsync("settings/mode", cancellationToken);
        await EnsureSuccess(response, "settings.mode", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationModeDto>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        logger.LogInformation("{Service} returned application mode {Mode}", ServiceName, result.Data?.IsMonolith);
        return result.Data!;
    }

    public async Task UpdateApplicationMode(ApplicationModeDto request, CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.application-mode");
        using var response = await client.PutAsJsonAsync("settings/mode", request, JsonOpts, cancellationToken);
        await EnsureSuccess(response, "settings.application-mode", cancellationToken);

        logger.LogInformation("{Service} updated application mode to {Mode}", ServiceName, request.IsMonolith);
    }

    public async Task<List<JobCandidate>> GetSimilarJobs(Guid jobId, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ai-service-v2.get-similar-jobs", ActivityKind.Internal);
        using var response = await client.GetAsync($"jobs/{jobId}/similar", cancellationToken);
        await EnsureSuccess(response, "ai-service-v2.get-similar-jobs", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<JobCandidate>>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        var data = result.Data ?? [];
        logger.LogInformation("{Service} returned {Count} matching jobs for job {JobId}", ServiceName, data.Count, jobId);
        return data;
    }

    public async Task<List<JobCandidate>> GetMatchingJobsForResumeAsync(Guid resumeId, int requestLimit, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ai-service-v2.get-matching-jobs", ActivityKind.Internal);
        EnrichActivity(null, "get-matching-jobs");
        activity?.SetTag("resume.id", resumeId);
        activity?.SetTag("matching.limit", requestLimit);

        using var response = await client.GetAsync($"resumes/{resumeId}/matching?limit={requestLimit}", cancellationToken);
        await EnsureSuccess(response, "ai-service-v2.get-matching-jobs", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<JobCandidate>>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        var data = result.Data ?? [];
        logger.LogInformation("{Service} returned {Count} matching jobs for resume {ResumeId}", ServiceName, data.Count, resumeId);
        return data;
    }

    public async Task<List<JobCandidate>> SearchJobs(string? query, string? location, string? jobType, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(location) && string.IsNullOrWhiteSpace(jobType))
            return [];

        using var activity = activityFactory.StartActivity("ai-service-v2.search-jobs", ActivityKind.Internal);

        var queryParams = new List<string> { $"limit={limit}" };
        if (!string.IsNullOrWhiteSpace(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(location)) queryParams.Add($"location={Uri.EscapeDataString(location)}");
        if (!string.IsNullOrWhiteSpace(jobType)) queryParams.Add($"jobType={Uri.EscapeDataString(jobType)}");

        using var response = await client.GetAsync($"jobs/search?{string.Join("&", queryParams)}", cancellationToken);
        await EnsureSuccess(response, "ai-service-v2.search-jobs", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<JobCandidate>>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        var data = result.Data ?? [];
        logger.LogInformation("{Service} returned {Count} jobs for query: {Query}", ServiceName, data.Count, query);
        return data;
    }

    public async Task<ReEmbedAllJobsResponse> ReEmbedAllJobs(CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.re-embed-jobs");
        using var response = await client.PostAsync("settings/re-embed-jobs", null, cancellationToken);
        await EnsureSuccess(response, "settings.re-embed-jobs", cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReEmbedAllJobsResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{ServiceName} returned empty JSON payload.");

        logger.LogInformation("{Service} re-embedded {Count} jobs", ServiceName, result.Data?.JobsProcessed);
        return result.Data!;
    }

    private static void EnrichActivity(Guid? companyId, string operation)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        if (companyId.HasValue)
            activity.SetTag("company.id", companyId);

        activity.SetTag("rpc.system", "http");
        activity.SetTag("rpc.service", ServiceName);
        activity.SetTag("rpc.method", operation);
        activity.SetTag("operation.type", "integration");
    }

    private async Task EnsureSuccess(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError("{Service} failed {Operation} with status {StatusCode}: {Body}",
            ServiceName, operation, (int)response.StatusCode, raw);

        throw new ExternalServiceException(
            service: ServiceName,
            operation: operation,
            statusCode: response.StatusCode,
            responseBody: raw);
    }
}
