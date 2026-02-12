using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Queries.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Application.Queries;

public class JobQueryService(DaprClient client, IConfiguration configuration, UserContextService accessor, ILogger<JobQueryService> _logger) : IJobQueryService
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<ApiResponse<List<JobResponse>>> ListAsync(Guid companyUId, CancellationToken ct)
    {
        var authHeader= accessor.GetHeader("Authorization");

        var request = client.CreateInvokeMethodRequest(HttpMethod.Get, "job-api", $"jobs/{companyUId}");
        request.Headers.Add("Authorization", authHeader?.Trim());
        return await DaprExtensions.Process(()=> client.InvokeMethodAsync<List<JobResponse>>(request, ct));

    }
    
    public async Task<ApiResponse<List<JobDraftResponse>>> ListDrafts(string companyId, CancellationToken ct = default)
    {
        try
        {
            var service = configuration.GetValue<string>("ai-source") ?? "ai-service-v2";

            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: service,
                methodName: $"drafts/{companyId}"
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

            if (service == "ai-service-v2")
            {
                var responses = JsonSerializer.Deserialize<ApiResponse<List<JobDraftResponse>>>(raw, JsonOpts);

                return responses ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service.");
            }
            var result = JsonSerializer.Deserialize<List<JobDraftResponse>>(raw, JsonOpts);

            return result is null ? throw new InvalidOperationException("Empty or invalid JSON from ai-service.") : new ApiResponse<List<JobDraftResponse>> { Data = result, Success = true, StatusCode = HttpStatusCode.OK };
        }catch (Exception e)
        {
            _logger.LogError(e, "Error generating job draft");
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