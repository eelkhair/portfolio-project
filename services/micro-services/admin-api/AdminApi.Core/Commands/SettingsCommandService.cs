using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Settings;
using AdminAPI.Contracts.Services;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands;

public partial class SettingsCommandService(DaprClient client, UserContextService accessor, ILogger<SettingsCommandService> logger) : ISettingsCommandService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<ApiResponse<GetProviderResponse>> GetProviderAsync(CancellationToken ct = default)
    {
        try
        {
            LogGettingProvider(logger);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: "ai-service-v2",
                methodName: "settings/provider"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogAiServiceError(logger, (int)resp.StatusCode, raw);
                throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<GetProviderResponse>>(raw, JsonOpts);

            Activity.Current?.SetTag("ai.provider", result?.Data?.Provider);
            Activity.Current?.SetTag("ai.model", result?.Data?.Model);

            LogProviderRetrieved(logger, result?.Data?.Provider ?? "unknown", result?.Data?.Model ?? "unknown");
            return new ApiResponse<GetProviderResponse>
            {
                Data = result?.Data,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogGetProviderError(logger, e);
            return new ApiResponse<GetProviderResponse>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
                    {
                        { "Error", [e.Message] }
                    }
                }
            };
        }
    }

    public async Task<ApiResponse<UpdateProviderResponse>> UpdateProviderAsync(UpdateProviderRequest request, CancellationToken ct = default)
    {
        Activity.Current?.SetTag("ai.provider", request.Provider);
        Activity.Current?.SetTag("ai.model", request.Model);

        try
        {
            LogUpdatingProvider(logger, request.Provider, request.Model);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId: "ai-service-v2",
                methodName: "settings/update-provider"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogAiServiceError(logger, (int)resp.StatusCode, raw);
                throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            LogProviderUpdated(logger, request.Provider, request.Model);
            return new ApiResponse<UpdateProviderResponse>
            {
                Data = new UpdateProviderResponse { Success = true },
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogUpdateProviderError(logger, e);
            return new ApiResponse<UpdateProviderResponse>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
                    {
                        { "Error", [e.Message] }
                    }
                }
            };
        }
    }

    public async Task<ApiResponse<ApplicationModeDto>> GetApplicationModeAsync(CancellationToken ct)
    {
        try
        {
            LogGettingApplicationMode(logger);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: "ai-service-v2",
                methodName: "settings/mode"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogAiServiceError(logger, (int)resp.StatusCode, raw);
                throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<ApplicationModeDto>>(raw, JsonOpts)!;

            Activity.Current?.SetTag("isMonolith", result?.Data?.IsMonolith);

            LogApplicationModeRetrieved(logger, result?.Data?.IsMonolith ?? false);
            return result!;
        }
        catch (Exception e)
        {
            LogGetApplicationModeError(logger, e);
            return new ApiResponse<ApplicationModeDto>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
                    {
                        { "Error", [e.Message] }
                    }
                }
            };
        }
    }

    public async Task<ApiResponse<ReEmbedJobsResponse>> ReEmbedJobsAsync(CancellationToken ct)
    {
        try
        {
            LogReEmbeddingJobs(logger);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Post,
                appId: "ai-service-v2",
                methodName: "settings/re-embed-jobs"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogAiServiceError(logger, (int)resp.StatusCode, raw);
                throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<ReEmbedJobsResponse>>(raw, JsonOpts);

            Activity.Current?.SetTag("jobs.processed", result?.Data?.JobsProcessed);

            LogReEmbedCompleted(logger, result?.Data?.JobsProcessed ?? 0);
            return new ApiResponse<ReEmbedJobsResponse>
            {
                Data = result?.Data,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogReEmbedJobsError(logger, e);
            return new ApiResponse<ReEmbedJobsResponse>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
                    {
                        { "Error", [e.Message] }
                    }
                }
            };
        }
    }

    public async Task<ApiResponse<ApplicationModeDto>> UpdateApplicationModeAsync(ApplicationModeDto request, CancellationToken ct)
    {
        Activity.Current?.SetTag("isMonolith", request.IsMonolith);

        try
        {
            LogUpdatingApplicationMode(logger, request.IsMonolith);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId: "ai-service-v2",
                methodName: "settings/mode"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (resp.IsSuccessStatusCode)
            {
                LogApplicationModeUpdated(logger, request.IsMonolith);
                return new ApiResponse<ApplicationModeDto>
                {
                    Data = request,
                    Success = true,
                    StatusCode = HttpStatusCode.OK
                };
            }
            LogAiServiceError(logger, (int)resp.StatusCode, raw);
            throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);

        }
        catch (Exception e)
        {
            LogUpdateApplicationModeError(logger, e);
            return new ApiResponse<ApplicationModeDto>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
                    {
                        { "Error", [e.Message] }
                    }
                }
            };
        }
    }

    [LoggerMessage(LogLevel.Information, "Getting AI provider settings")]
    static partial void LogGettingProvider(ILogger logger);

    [LoggerMessage(LogLevel.Information, "AI provider retrieved: {Provider} / {Model}")]
    static partial void LogProviderRetrieved(ILogger logger, string provider, string model);

    [LoggerMessage(LogLevel.Information, "Updating AI provider to {Provider} / {Model}")]
    static partial void LogUpdatingProvider(ILogger logger, string provider, string model);

    [LoggerMessage(LogLevel.Information, "AI provider updated: {Provider} / {Model}")]
    static partial void LogProviderUpdated(ILogger logger, string provider, string model);

    [LoggerMessage(LogLevel.Information, "Getting application mode")]
    static partial void LogGettingApplicationMode(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Application mode retrieved: IsMonolith={IsMonolith}")]
    static partial void LogApplicationModeRetrieved(ILogger logger, bool isMonolith);

    [LoggerMessage(LogLevel.Information, "Updating application mode: IsMonolith={IsMonolith}")]
    static partial void LogUpdatingApplicationMode(ILogger logger, bool isMonolith);

    [LoggerMessage(LogLevel.Information, "Application mode updated: IsMonolith={IsMonolith}")]
    static partial void LogApplicationModeUpdated(ILogger logger, bool isMonolith);

    [LoggerMessage(LogLevel.Information, "Re-embedding jobs via ai-service-v2")]
    static partial void LogReEmbeddingJobs(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Re-embed completed: {JobsProcessed} jobs processed")]
    static partial void LogReEmbedCompleted(ILogger logger, int jobsProcessed);

    [LoggerMessage(LogLevel.Error, "ai-service-v2 returned {StatusCode}: {Body}")]
    static partial void LogAiServiceError(ILogger logger, int statusCode, string body);

    [LoggerMessage(LogLevel.Error, "Error getting AI provider settings")]
    static partial void LogGetProviderError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error updating AI provider settings")]
    static partial void LogUpdateProviderError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error getting application mode")]
    static partial void LogGetApplicationModeError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error updating application mode")]
    static partial void LogUpdateApplicationModeError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error re-embedding jobs")]
    static partial void LogReEmbedJobsError(ILogger logger, Exception exception);
}
