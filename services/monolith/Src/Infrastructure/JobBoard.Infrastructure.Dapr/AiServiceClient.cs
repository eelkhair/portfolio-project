using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Actions.Settings.Models;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.infrastructure.Dapr;

public sealed class AiServiceClient(
    DaprClient client,
    IUserAccessor accessor,
    IConfiguration configuration,
    ILogger<AiServiceClient> logger)
    : IAiServiceClient
{
    private const string AiServiceV1 = "ai-service";
    private const string AiServiceV2 = "ai-service-v2";
    private string AiSource => configuration.GetValue<string>("ai-source") ?? AiServiceV2;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<List<JobDraftResponse>> ListDrafts(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        EnrichActivity(companyId, "drafts.list", AiServiceV1);

        var request = CreateRequest(
            HttpMethod.Get,
            $"drafts/{companyId}",
            AiServiceV1);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.list", cancellationToken, AiServiceV1);

        var drafts = await response.Content
                         .ReadFromJsonAsync<List<JobDraftResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV1} returned empty JSON payload.");

        Activity.Current?.SetTag("ai.drafts.count", drafts.Count);

        logger.LogInformation(
            "ai-service returned {DraftCount} drafts for company {CompanyId}",
            drafts.Count,
            companyId);

        return drafts;
    }

    public async Task<JobRewriteResponse> RewriteItem(
        JobRewriteRequest requestModel,
        CancellationToken cancellationToken)
    {
        EnrichActivity(null, "drafts.rewrite.item", AiServiceV1);

        var request = CreateRequest(
            HttpMethod.Put,
            "drafts/rewrite/item",
            AiServiceV1);

        request.Content = JsonContent.Create(requestModel, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.rewrite.item", cancellationToken, AiServiceV1);

        var result = await response.Content
                         .ReadFromJsonAsync<JobRewriteResponse>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{AiServiceV1} returned empty JSON payload.");

        return result;
    }

    public async Task<JobGenResponse> GenerateDraft(
        Guid companyId,
        JobGenRequest requestModel,
        CancellationToken cancellationToken)
    {
        var serviceName = AiSource;
        EnrichActivity(companyId, "drafts.generate", serviceName);

        var request = CreateRequest(
            HttpMethod.Post,
            $"drafts/{companyId}/generate",
            serviceName);

        request.Content = JsonContent.Create(requestModel, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.generate", cancellationToken, serviceName);

        var result = await response.Content
                         .ReadFromJsonAsync<ApiResponse<JobGenResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException($"{serviceName} returned empty JSON payload.");

        Activity.Current?.SetTag("ai.draft.id", result.Data?.DraftId);
        Activity.Current?.SetTag("ai.draft.title", result.Data?.Title);

        logger.LogInformation(
            "{ServiceName} generated draft {DraftId} with title '{Title}' for company {CompanyId}",
            serviceName,
            result.Data?.DraftId,
            result.Data?.Title,
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