using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Dapr.Client;
using JobBoard.Application.Actions.Jobs.Models;
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

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = client.CreateInvokeMethodRequest(
            method,
            appId: ServiceName,
            methodName: path);

        request.Headers.TryAddWithoutValidation("Authorization", accessor.Token);
        return request;
    }

    private static void EnrichActivity(Guid? companyId, string operation)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        if (companyId.HasValue)
            activity.SetTag("company.id", companyId);

        activity.SetTag("rpc.system", "dapr");
        activity.SetTag("rpc.service", ServiceName);
        activity.SetTag("rpc.method", operation);
        activity.SetTag("operation.type", "integration");
    }

    private async Task ThrowExternalServiceError(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogError(
            "ai-service failed {Operation} with status {StatusCode}: {Body}",
            operation,
            (int)response.StatusCode,
            raw);

        throw new ExternalServiceException(
            service: ServiceName,
            operation: operation,
            statusCode: response.StatusCode,
            responseBody: raw);
    }
}