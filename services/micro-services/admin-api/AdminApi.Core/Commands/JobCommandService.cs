using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Services;
using AdminAPI.Contracts.Models.Jobs.Events;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using JobAPI.Contracts.Models.Jobs.Responses;
using Microsoft.Extensions.Logging;

namespace AdminApi.Application.Commands;

public partial class JobCommandService(DaprClient client, UserContextService accessor, IMessageSender sender, ILogger<JobCommandService> logger) : IJobCommandService
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<ApiResponse<JobDraftResponse>> CreateDraft(string companyId, JobDraftRequest request, CancellationToken ct = default)
    {
        try
        {
            LogCreatingDraft(logger, companyId);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId: "job-api",
                methodName: $"api/drafts/{companyId}"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            var userId = accessor.GetCurrentUser() ?? "unknown";
            var envelope = new EventDto<JobDraftRequest>(userId, Guid.NewGuid().ToString(), request);
            req.Content = JsonContent.Create(envelope, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"job-api {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var draft = JsonSerializer.Deserialize<JobDraftResponse>(raw, JsonOpts);
            LogDraftCreated(logger, companyId);

            return new ApiResponse<JobDraftResponse>
            {
                Data = draft,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }catch (Exception e)
        {
            LogCreateDraftError(logger, e);
            return new ApiResponse<JobDraftResponse> { Success = false, StatusCode = HttpStatusCode.InternalServerError, Exceptions = new ApiError()
            {
                Message = e.Message,
                Errors = new Dictionary<string, string[]>()
                {
                    {"Error", [e.Message]}
                }
            }};
        }
    }
    public async Task<ApiResponse<JobRewriteResponse>> RewriteItem(JobRewriteRequest request, CancellationToken ct)
    {
        try
        {
            LogRewritingItem(logger);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId: "ai-service-v2",
                methodName: $"drafts/rewrite/item"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogAiServiceError(logger, (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<JobRewriteResponse>>(raw, JsonOpts);
            LogRewriteCompleted(logger);

            return result ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service-v2.");
        }
        catch (Exception e)
        {
            LogRewriteItemError(logger, e);
            return new ApiResponse<JobRewriteResponse> { Success = false, StatusCode = HttpStatusCode.InternalServerError, Exceptions = new ApiError()
            {
                Message = e.Message,
                Errors = new Dictionary<string, string[]>()
                {
                    {"Error", [e.Message]}
                }
            }};
        }
    }

    public async Task<ApiResponse<JobResponse>> CreateJob(JobCreateRequest request, CancellationToken ct)
    {
        try
        {
            LogCreatingJob(logger);
            var req = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "api/jobs");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            var userId = accessor.GetCurrentUser() ?? "unknown";
            var envelope = new EventDto<JobCreateRequest>(userId, Guid.NewGuid().ToString(), request);
            req.Content = JsonContent.Create(envelope, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<JobResponse>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<JobResponse>(raw, JsonOpts);

            if (result is { } job)
            {
                await sender.SendEventAsync(PubSubNames.RabbitMq, "job.published.v2",
                    accessor.GetCurrentUser() ?? "unknown",
                    new JobPublishedEvent
                    {
                        UId = job.UId,
                        Title = job.Title,
                        CompanyUId = job.CompanyUId,
                        CompanyName = job.CompanyName,
                        Location = job.Location,
                        JobType = job.JobType.ToString(),
                        AboutRole = job.AboutRole,
                        SalaryRange = job.SalaryRange,
                        Responsibilities = job.Responsibilities,
                        Qualifications = job.Qualifications,
                        CreatedAt = job.CreatedAt,
                        UpdatedAt = job.UpdatedAt,
                        DraftId = request.DraftId,
                        DeleteDraft = request.DeleteDraft
                    }, ct);

                LogJobCreated(logger, job.UId);
            }

            return new ApiResponse<JobResponse>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogCreateJobError(logger, e);
            return new ApiResponse<JobResponse>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]> { { "Error", [e.Message] } }
                }
            };
        }
    }

    public async Task DeleteDraft(string companyId, Guid draftId, CancellationToken ct)
    {
        try
        {
            LogDeletingDraft(logger, draftId, companyId);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Delete,
                appId: "job-api",
                methodName: $"api/drafts/{draftId}"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            await client.InvokeMethodAsync(req, ct);
            LogDraftDeleted(logger, draftId);
        }
        catch (Exception e)
        {
            LogDeleteDraftError(logger, e, draftId, companyId);
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information, "Creating job draft for company {CompanyId}")]
    static partial void LogCreatingDraft(ILogger logger, string companyId);

    [LoggerMessage(LogLevel.Information, "Job draft created for company {CompanyId}")]
    static partial void LogDraftCreated(ILogger logger, string companyId);

    [LoggerMessage(LogLevel.Information, "Rewriting job item via ai-service-v2")]
    static partial void LogRewritingItem(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Job item rewrite completed")]
    static partial void LogRewriteCompleted(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Creating job via job-api")]
    static partial void LogCreatingJob(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Job created: {JobUId}")]
    static partial void LogJobCreated(ILogger logger, Guid jobUId);

    [LoggerMessage(LogLevel.Information, "Deleting draft {DraftId} for company {CompanyId}")]
    static partial void LogDeletingDraft(ILogger logger, Guid draftId, string companyId);

    [LoggerMessage(LogLevel.Information, "Draft deleted: {DraftId}")]
    static partial void LogDraftDeleted(ILogger logger, Guid draftId);

    [LoggerMessage(LogLevel.Error, "job-api returned {StatusCode}: {Body}")]
    static partial void LogJobApiError(ILogger logger, int statusCode, string body);

    [LoggerMessage(LogLevel.Error, "ai-service-v2 returned {StatusCode}: {Body}")]
    static partial void LogAiServiceError(ILogger logger, int statusCode, string body);

    [LoggerMessage(LogLevel.Error, "Error creating job draft")]
    static partial void LogCreateDraftError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error rewriting job item")]
    static partial void LogRewriteItemError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error creating job")]
    static partial void LogCreateJobError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error deleting draft {DraftId} for company {CompanyId}")]
    static partial void LogDeleteDraftError(ILogger logger, Exception exception, Guid draftId, string companyId);
}
