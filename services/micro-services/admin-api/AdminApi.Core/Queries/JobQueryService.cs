using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Jobs.Responses;
using AdminAPI.Contracts.Services;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Queries;

public partial class JobQueryService(DaprClient client, UserContextService accessor, ILogger<JobQueryService> logger) : IJobQueryService
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<ApiResponse<List<JobResponse>>> ListAsync(Guid companyUId, CancellationToken ct)
    {
        try
        {
            LogListingJobs(logger, companyUId);
            var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", $"api/jobs/{companyUId}");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<List<JobResponse>>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<List<JobResponse>>(raw, JsonOpts);
            LogJobsListed(logger, companyUId, result?.Count ?? 0);
            return new ApiResponse<List<JobResponse>>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogListJobsError(logger, e, companyUId);
            return new ApiResponse<List<JobResponse>>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { { "Error", [e.Message] } }
                }
            };
        }
    }

    public async Task<ApiResponse<List<CompanyJobSummaryResponse>>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
        try
        {
            LogListingCompanyJobSummaries(logger);
            var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", "api/companies/job-summaries");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<List<CompanyJobSummaryResponse>>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<List<CompanyJobSummaryResponse>>(raw, JsonOpts);
            LogCompanyJobSummariesListed(logger, result?.Count ?? 0);
            return new ApiResponse<List<CompanyJobSummaryResponse>>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogListCompanyJobSummariesError(logger, e);
            return new ApiResponse<List<CompanyJobSummaryResponse>>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { { "Error", [e.Message] } }
                }
            };
        }
    }

    public async Task<ApiResponse<List<JobDraftResponse>>> ListDrafts(string companyId, CancellationToken ct = default)
    {
        try
        {
            LogListingDrafts(logger, companyId);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: "job-api",
                methodName: $"api/drafts/{companyId}"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"job-api {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var drafts = JsonSerializer.Deserialize<List<JobDraftResponse>>(raw, JsonOpts);
            LogDraftsListed(logger, companyId, drafts?.Count ?? 0);

            return new ApiResponse<List<JobDraftResponse>>
            {
                Data = drafts ?? [],
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogListDraftsError(logger, e, companyId);
            return new ApiResponse<List<JobDraftResponse>>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError()
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    {"Error", [e.Message]}
                }
                }
            };
        }
    }

    public async Task<ApiResponse<JobDraftResponse?>> GetDraft(Guid draftId, CancellationToken ct)
    {
        try
        {
            LogGettingDraft(logger, draftId);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: "job-api",
                methodName: $"api/drafts/detail/{draftId}"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);
                return new ApiResponse<JobDraftResponse?>
                {
                    Data = null,
                    Success = false,
                    StatusCode = resp.StatusCode
                };
            }

            var draft = JsonSerializer.Deserialize<JobDraftResponse>(raw, JsonOpts);
            LogDraftRetrieved(logger, draftId);

            return new ApiResponse<JobDraftResponse?>
            {
                Data = draft,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogGetDraftError(logger, e, draftId);
            return new ApiResponse<JobDraftResponse?>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { { "Error", [e.Message] } }
                }
            };
        }
    }

    [LoggerMessage(LogLevel.Information, "Listing jobs for company {CompanyUId}")]
    static partial void LogListingJobs(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Jobs listed for company {CompanyUId}: {Count} found")]
    static partial void LogJobsListed(ILogger logger, Guid companyUId, int count);

    [LoggerMessage(LogLevel.Information, "Listing company job summaries")]
    static partial void LogListingCompanyJobSummaries(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Company job summaries listed: {Count} found")]
    static partial void LogCompanyJobSummariesListed(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Listing drafts for company {CompanyId}")]
    static partial void LogListingDrafts(ILogger logger, string companyId);

    [LoggerMessage(LogLevel.Information, "Drafts listed for company {CompanyId}: {Count} found")]
    static partial void LogDraftsListed(ILogger logger, string companyId, int count);

    [LoggerMessage(LogLevel.Information, "Getting draft {DraftId}")]
    static partial void LogGettingDraft(ILogger logger, Guid draftId);

    [LoggerMessage(LogLevel.Information, "Draft retrieved: {DraftId}")]
    static partial void LogDraftRetrieved(ILogger logger, Guid draftId);

    [LoggerMessage(LogLevel.Error, "job-api returned {StatusCode}: {Body}")]
    static partial void LogJobApiError(ILogger logger, int statusCode, string body);

    [LoggerMessage(LogLevel.Error, "Error listing jobs for company {CompanyUId}")]
    static partial void LogListJobsError(ILogger logger, Exception exception, Guid companyUId);

    [LoggerMessage(LogLevel.Error, "Error listing company job summaries")]
    static partial void LogListCompanyJobSummariesError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error listing drafts for company {CompanyId}")]
    static partial void LogListDraftsError(ILogger logger, Exception exception, string companyId);

    [LoggerMessage(LogLevel.Error, "Error getting draft {DraftId}")]
    static partial void LogGetDraftError(ILogger logger, Exception exception, Guid draftId);
}
