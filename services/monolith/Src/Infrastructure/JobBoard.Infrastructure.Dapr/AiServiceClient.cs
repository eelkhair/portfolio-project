using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobBoard.Application.Actions.Public;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace JobBoard.infrastructure.Dapr;

public sealed class AiServiceClient(
    DaprClient client,
    IActivityFactory activityFactory,
    IUserAccessor accessor,
    ILogger<AiServiceClient> logger)
    : IAiServiceClient
{
    private const string AiServiceV2 = "ai-service-v2";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<List<DraftResponse>> ListDrafts(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        EnrichActivity(companyId, "drafts.list", AiServiceV2);

        var request = CreateRequest(
            HttpMethod.Get,
            $"drafts/{companyId}",
            AiServiceV2);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.list", cancellationToken, AiServiceV2);

        var results = await response.Content
                         .ReadFromJsonAsync<ApiResponse<List<DraftResponse>>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        Activity.Current?.SetTag("ai.drafts.count", results.Data?.Count);

        logger.LogInformation(
            "ai-service-v2 returned {DraftCount} drafts for company {CompanyId}",
            results.Data?.Count,
            companyId);

        return results.Data!;
    }

    public async Task<DraftRewriteResponse> RewriteItem(
        DraftItemRewriteRequest requestModel,
        CancellationToken cancellationToken)
    {
        EnrichActivity(null, "drafts.rewrite.item", AiServiceV2);

        var request = CreateRequest(
            HttpMethod.Put,
            "drafts/rewrite/item",
            AiServiceV2);

        request.Content = JsonContent.Create(requestModel, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.rewrite.item", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<DraftRewriteResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        return result.Data!;
    }

    public async Task<DraftGenResponse> GenerateDraft(
        Guid companyId,
        DraftGenRequest requestModel,
        CancellationToken cancellationToken)
    {
        EnrichActivity(companyId, "drafts.generate", AiServiceV2);

        var request = CreateRequest(
            HttpMethod.Post,
            $"drafts/{companyId}/generate",
            AiServiceV2);

        request.Content = JsonContent.Create(requestModel, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.generate", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<DraftGenResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        Activity.Current?.SetTag("ai.draft.id", result.Data?.DraftId);
        Activity.Current?.SetTag("ai.draft.title", result.Data?.Title);

        logger.LogInformation(
            "{ServiceName} generated draft {DraftId} with title '{Title}' for company {CompanyId}",
            AiServiceV2,
            result.Data?.DraftId,
            result.Data?.Title,
            companyId);

        return result.Data!;
    }

    public async Task<DraftResponse> SaveDraft(
        Guid companyId,
        DraftResponse draft,
        CancellationToken cancellationToken)
    {
        EnrichActivity(companyId, "drafts.upsert", AiServiceV2);

        var request = CreateRequest(
            HttpMethod.Put,
            $"drafts/{companyId}/upsert",
            AiServiceV2);

        request.Content = JsonContent.Create(draft, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.upsert", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<DraftResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        logger.LogInformation(
            "{ServiceName} saved draft {DraftId} for company {CompanyId}",
            AiServiceV2,
            result.Data?.Id,
            companyId);

        return result.Data!;
    }

    public async Task<ProviderSettings> GetProvider(CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.get-provider", AiServiceV2);

        var request = CreateRequest(
            HttpMethod.Get,
            "settings/provider",
            AiServiceV2);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "settings.get-provider", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<ProviderSettings>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        logger.LogInformation(
            "ai-service-v2 returned provider {Provider} with model {Model}",
            result.Data?.Provider,
            result.Data?.Model);

        return result.Data!;
    }

    public async Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.update-provider", AiServiceV2);

        var httpRequest = CreateRequest(
            HttpMethod.Put,
            "settings/update-provider",
            AiServiceV2);

        httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "settings.update-provider", cancellationToken, AiServiceV2);

        logger.LogInformation(
            "{ServiceName} updated provider to {Provider} with model {Model}",
            AiServiceV2,
            request.Provider,
            request.Model);
    }
    
    public async Task<ApplicationModeDto> GetApplicationMode(CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.mode", AiServiceV2);

        var request = CreateRequest(
            HttpMethod.Get,
            "settings/mode",
            AiServiceV2);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "settings.mode", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<ApplicationModeDto>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        logger.LogInformation(
            "ai-service-v2 returned application mode {Mode}",
            result.Data?.IsMonolith);

        return result.Data!;
    }

    public async Task<List<JobCandidate>> GetSimilarJobs(Guid jobId, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ai-service-v2.get-similar-jobs", ActivityKind.Internal);

        var request = CreateRequest(
            HttpMethod.Get,
            $"jobs/{jobId}/similar",
            AiServiceV2);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "ai-service-v2.get-similar-jobs", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<List<JobCandidate>>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        var data = result.Data ?? [];
        logger.LogInformation(
            "ai-service-v2 returned {Count} matching jobs for  job {JobId}",
            data.Count, jobId);

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

        var request = CreateRequest(
            HttpMethod.Get,
            $"jobs/search?{string.Join("&", queryParams)}",
            AiServiceV2);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "ai-service-v2.search-jobs", cancellationToken, AiServiceV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<List<JobCandidate>>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV2} returned empty JSON payload.");

        var data = result.Data ?? [];
        logger.LogInformation(
            "ai-service-v2 returned {Count} jobs for query: {Query}",
            data.Count, query);

        return data;
    }

    public async Task UpdateApplicationMode(ApplicationModeDto request, CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.application-mode", AiServiceV2);

        var httpRequest = CreateRequest(
            HttpMethod.Put,
            "settings/mode",
            AiServiceV2);

        httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "settings.application-mode", cancellationToken, AiServiceV2);

        logger.LogInformation(
            "{ServiceName} updated application mode to {Mode}",
            AiServiceV2,
            request.IsMonolith);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string serviceName)
    {
        var request = client.CreateInvokeMethodRequest(
            method,
            appId: serviceName,
            methodName: path);

        request.Headers.TryAddWithoutValidation("Authorization", accessor.Token);
        return request;
    }

    private static void EnrichActivity(Guid? companyId, string operation, string serviceName)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        if (companyId.HasValue)
            activity.SetTag("company.id", companyId);

        activity.SetTag("rpc.system", "dapr");
        activity.SetTag("rpc.service", serviceName);
        activity.SetTag("rpc.method", operation);
        activity.SetTag("operation.type", "integration");
    }

    private async Task ThrowExternalServiceError(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken,
        string serviceName)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogError(
            "{Service} failed {Operation} with status {StatusCode}: {Body}",
            serviceName,
            operation,
            (int)response.StatusCode,
            raw);

        throw new ExternalServiceException(
            service: serviceName,
            operation: operation,
            statusCode: response.StatusCode,
            responseBody: raw);
    }
}