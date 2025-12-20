using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Users;
using JobBoard.infrastructure.Dapr;
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
        EnrichActivity(companyId, "drafts.list");

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

    private static void EnrichActivity(Guid companyId, string operationName)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        activity.SetTag("company.id", companyId);
        activity.SetTag("rpc.system", "dapr");
        activity.SetTag("rpc.service", ServiceName);
        activity.SetTag("rpc.method", operationName);
        activity.SetTag("operation.type", "integration");
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
