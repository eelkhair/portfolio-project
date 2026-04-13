using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Dashboard;
using AdminAPI.Contracts.Services;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Queries;

public partial class DashboardQueryService(DaprClient client, UserContextService accessor, ILogger<DashboardQueryService> logger)
    : IDashboardQueryService
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<ApiResponse<DashboardResponse>> GetDashboardAsync(CancellationToken ct)
    {
        try
        {
            LogFetchingDashboard(logger);
            var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", "api/dashboard");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogJobApiError(logger, (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<DashboardResponse>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<DashboardResponse>(raw, JsonOpts);
            LogDashboardFetched(logger);
            return new ApiResponse<DashboardResponse>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            LogFetchDashboardError(logger, e);
            return new ApiResponse<DashboardResponse>
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

    [LoggerMessage(LogLevel.Information, "Fetching dashboard from job-api")]
    static partial void LogFetchingDashboard(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Dashboard fetched successfully")]
    static partial void LogDashboardFetched(ILogger logger);

    [LoggerMessage(LogLevel.Error, "job-api returned {StatusCode}: {Body}")]
    static partial void LogJobApiError(ILogger logger, int statusCode, string body);

    [LoggerMessage(LogLevel.Error, "Error fetching dashboard from job-api")]
    static partial void LogFetchDashboardError(ILogger logger, Exception exception);
}
