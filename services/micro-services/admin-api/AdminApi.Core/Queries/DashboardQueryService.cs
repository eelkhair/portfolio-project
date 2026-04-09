using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Dashboard;
using AdminAPI.Contracts.Services;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;

namespace AdminApi.Application.Queries;

public class DashboardQueryService(DaprClient client, UserContextService accessor, ILogger<DashboardQueryService> logger)
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
            var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", "api/dashboard");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogError("job-api returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<DashboardResponse>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<DashboardResponse>(raw, JsonOpts);
            return new ApiResponse<DashboardResponse>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching dashboard from job-api");
            return new ApiResponse<DashboardResponse>
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
}
