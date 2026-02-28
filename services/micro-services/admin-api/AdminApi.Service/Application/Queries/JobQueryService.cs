using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Queries;

public class JobQueryService(DaprClient client, UserContextService accessor, ILogger<JobQueryService> _logger) : IJobQueryService
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
            var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", $"jobs/{companyUId}");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("job-api returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<List<JobResponse>>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<List<JobResponse>>(raw, JsonOpts);
            return new ApiResponse<List<JobResponse>>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error listing jobs for company {CompanyUId}", companyUId);
            return new ApiResponse<List<JobResponse>>
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

    public async Task<ApiResponse<List<CompanyJobSummaryResponse>>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
        try
        {
            var req = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", "companies/job-summaries");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("job-api returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<List<CompanyJobSummaryResponse>>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<List<CompanyJobSummaryResponse>>(raw, JsonOpts);
            return new ApiResponse<List<CompanyJobSummaryResponse>>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error listing company job summaries");
            return new ApiResponse<List<CompanyJobSummaryResponse>>
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

    public async Task<ApiResponse<List<JobDraftResponse>>> ListDrafts(string companyId, CancellationToken ct = default)
    {
        try
        {
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: "ai-service-v2",
                methodName: $"drafts/{companyId}"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);


            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("ai-service-v2 returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<List<JobDraftResponse>>>(raw, JsonOpts);

            return result ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service-v2.");
        }catch (Exception e)
        {
            _logger.LogError(e, "Error listing drafts");
            return new ApiResponse<List<JobDraftResponse>> { Success = false, StatusCode = HttpStatusCode.InternalServerError, Exceptions = new ApiError()
            {
                Message = e.Message,
                Errors = new Dictionary<string, string[]>()
                {
                    {"Error", [e.Message]}
                }
            }};
        }
    }

}
