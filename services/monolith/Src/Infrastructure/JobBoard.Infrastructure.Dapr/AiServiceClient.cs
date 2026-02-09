using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Dapr.Client;
using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Actions.Settings.Models;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.Logging;

namespace JobBoard.infrastructure.Dapr;

public sealed class AiServiceClient(
    DaprClient client,
    IUserAccessor accessor,
    ILogger<AiServiceClient> logger)
    : IAiServiceClient
{
    private const string ServiceName = "ai-service";
    private const string ServiceNameV2 = "ai-service-v2";

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
        EnrichActivity(companyId, "drafts.list");

        var request = CreateRequest(
            HttpMethod.Get,
            $"drafts/{companyId}");

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.list", cancellationToken);

        var drafts = await response.Content
                         .ReadFromJsonAsync<List<JobDraftResponse>>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException("ai-service returned empty JSON payload.");

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
        EnrichActivity(null, "drafts.rewrite.item");

        var request = CreateRequest(
            HttpMethod.Put,
            "drafts/rewrite/item");

        request.Content = JsonContent.Create(requestModel, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "drafts.rewrite.item", cancellationToken);

        var result = await response.Content
                         .ReadFromJsonAsync<JobRewriteResponse>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException("ai-service returned empty JSON payload.");

        return result;
    }

    public async Task<ProviderSettings> GetProvider(CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.get-provider", ServiceNameV2);

        var request = CreateRequest(
            HttpMethod.Get,
            "settings/provider",
            ServiceNameV2);

        using var response =
            await client.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "settings.get-provider", cancellationToken, ServiceNameV2);

        var result = await response.Content
                         .ReadFromJsonAsync<ProviderSettings>(JsonOpts, cancellationToken)
                     ?? throw new InvalidOperationException("ai-service-v2 returned empty JSON payload.");

        logger.LogInformation(
            "ai-service-v2 returned provider {Provider} with model {Model}",
            result.Provider,
            result.Model);

        return result;
    }

    public async Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        EnrichActivity(null, "settings.update-provider", ServiceNameV2);

        var httpRequest = CreateRequest(
            HttpMethod.Put,
            "settings/update-provider",
            ServiceNameV2);

        httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

        using var response =
            await client.InvokeMethodWithResponseAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowExternalServiceError(response, "settings.update-provider", cancellationToken, ServiceNameV2);

        logger.LogInformation(
            "ai-service-v2 updated provider to {Provider} with model {Model}",
            request.Provider,
            request.Model);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string? serviceName = null)
    {
        var request = client.CreateInvokeMethodRequest(
            method,
            appId: serviceName ?? ServiceName,
            methodName: path);

        request.Headers.TryAddWithoutValidation("Authorization", accessor.Token);
        return request;
    }

    private static void EnrichActivity(Guid? companyId, string operation, string? serviceName = null)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        if (companyId.HasValue)
            activity.SetTag("company.id", companyId);

        activity.SetTag("rpc.system", "dapr");
        activity.SetTag("rpc.service", serviceName ?? ServiceName);
        activity.SetTag("rpc.method", operation);
        activity.SetTag("operation.type", "integration");
    }

    private async Task ThrowExternalServiceError(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken,
        string? serviceName = null)
    {
        var service = serviceName ?? ServiceName;
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogError(
            "{Service} failed {Operation} with status {StatusCode}: {Body}",
            service,
            operation,
            (int)response.StatusCode,
            raw);

        throw new ExternalServiceException(
            service: service,
            operation: operation,
            statusCode: response.StatusCode,
            responseBody: raw);
    }
}