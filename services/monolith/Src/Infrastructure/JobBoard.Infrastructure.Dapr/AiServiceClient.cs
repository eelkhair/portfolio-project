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

namespace JobBoard.Infrastructure.Dapr;

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
        var activity = Activity.Current;
        
        activity?.SetTag("company.id", companyId);
        activity?.SetTag("rpc.system", "dapr");
        activity?.SetTag("rpc.service", ServiceName);
        activity?.SetTag("rpc.method", "drafts.list");
        activity?.SetTag("operation.type", "integration");

        var request = client.CreateInvokeMethodRequest(
            HttpMethod.Get,
            appId: ServiceName,
            methodName: $"drafts/{companyId}"
        );

        request.Headers.TryAddWithoutValidation("Authorization", accessor.Token);

        try
        {
            using var response =
                await client.InvokeMethodWithResponseAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                await ThrowExternalServiceError(response,"drafts.list",cancellationToken);

            var drafts = await response.Content.ReadFromJsonAsync<List<JobDraftResponse>>(
                             JsonOpts, cancellationToken)
                         ?? throw new InvalidOperationException(
                             "ai-service returned empty JSON payload.");

            Activity.Current?.SetTag("ai.drafts.count", drafts.Count);

            logger.LogInformation(
                "ai-service returned {DraftCount} drafts for company {CompanyId}",
                drafts.Count,
                companyId);

            return drafts;
        }
        catch (Exception ex)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex,
                "Failed to list drafts from ai-service for company {CompanyId}",
                companyId);
            throw;
        }
    }

    public async Task<JobRewriteResponse> RewriteItem(JobRewriteRequest jobRewriteRequest, CancellationToken cancellationToken)
    {
        try
        {
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId: "ai-service",
                methodName: $"drafts/rewrite/item"
            );

            req.Headers.TryAddWithoutValidation("Authorization", accessor.Token);
            req.Content = JsonContent.Create(jobRewriteRequest, options: JsonOpts);
            
            using var resp = await client.InvokeMethodWithResponseAsync(req, cancellationToken);

            var raw = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {

                logger.LogError("ai-service returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<JobRewriteResponse>(raw, JsonOpts);

            if (result is null)
                throw new InvalidOperationException("Empty or invalid JSON from ai-service.");

            return result;
        }
        catch (Exception ex)
        {
            
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex,
                "Failed to rewrite item" );
            throw;
        }
    }

  

    private async Task ThrowExternalServiceError(
        HttpResponseMessage response,
        string operationName,
        CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogError(
            "ai-service failed with status {StatusCode}: {Body}",
            (int)response.StatusCode,
            raw);

        throw new ExternalServiceException(
            service: ServiceName,
            operation: operationName,
            statusCode: response.StatusCode,
            responseBody: raw);
    }
}
