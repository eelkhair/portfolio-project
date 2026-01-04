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
                appId: "ai-service",
                methodName: $"drafts/{companyId}/upsert"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);


            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {

                _logger.LogError("ai-service returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<JobDraftResponse>(raw, JsonOpts);

            if (result is null)
                throw new InvalidOperationException("Empty or invalid JSON from ai-service.");

            return new ApiResponse<JobDraftResponse> { Data = result, Success = true, StatusCode = HttpStatusCode.OK };
        }catch (Exception e)
        {
            _logger.LogError(e, "Error generating job draft");
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
    public async Task<ApiResponse<bool>> DeleteDraft(string draftId, string companyId, CancellationToken ct)
    {
        try
        {
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Delete,
                appId: "ai-service",
                methodName: $"drafts/{companyId}/{draftId}"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);
            
            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {

                _logger.LogError("ai-service returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<JobDraftResponse>(raw, JsonOpts);

            if (result is null)
                throw new InvalidOperationException("Empty or invalid JSON from ai-service.");

            return new ApiResponse<bool> { Success = true, StatusCode = HttpStatusCode.OK };
        }catch (Exception e)
        {
            _logger.LogError(e, "Error generating job draft");
            return new ApiResponse<bool> { Success = false, StatusCode = HttpStatusCode.InternalServerError, Exceptions = new ApiError()
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
                appId: "ai-service",
                methodName: $"drafts/rewrite/item"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);


            req.Content = JsonContent.Create(request, options: JsonOpts);
            
            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {

                _logger.LogError("ai-service returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<JobRewriteResponse>(raw, JsonOpts);

            if (result is null)
                throw new InvalidOperationException("Empty or invalid JSON from ai-service.");

            return new ApiResponse<JobRewriteResponse> { Data = result, Success = true, StatusCode = HttpStatusCode.OK };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error generating job draft");
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
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "job-api", "jobs");
        if (accessor != null)
        {
            message.Headers.Add("Authorization", accessor?.GetHeader("Authorization"));
        }
       
        message.Content=  JsonContent.Create(request);
        return await DaprExtensions.Process(() =>
            client.InvokeMethodAsync<JobResponse>(message,cancellationToken: ct));
    }

   
}