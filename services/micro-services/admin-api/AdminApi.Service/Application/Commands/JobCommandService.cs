using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Commands;

public class JobCommandService(DaprClient client, UserContextService accessor, ILogger<JobCommandService> _logger) : IJobCommandService
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
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId: "ai-service-v2",
                methodName: $"drafts/{companyId}/upsert"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("ai-service-v2 returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var response = JsonSerializer.Deserialize<ApiResponse<JobDraftResponse>>(raw, JsonOpts);

            return response ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service-v2.");
        }catch (Exception e)
        {
            _logger.LogError(e, "Error creating job draft");
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
                _logger.LogError("ai-service-v2 returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<JobRewriteResponse>>(raw, JsonOpts);

            return result ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service-v2.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error rewriting job item");
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
            var req = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "jobs");

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);

            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("job-api returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);
                var error = JsonSerializer.Deserialize<ApiError>(raw, JsonOpts);
                return new ApiResponse<JobResponse>
                {
                    Success = false,
                    StatusCode = resp.StatusCode,
                    Exceptions = error ?? new ApiError { Message = raw }
                };
            }

            var result = JsonSerializer.Deserialize<JobResponse>(raw, JsonOpts);
            return new ApiResponse<JobResponse>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating job");
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


}
