using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands;

public class OpenAICommandService(DaprClient client, UserContextService accessor, IConfiguration configuration, ILogger<OpenAICommandService> _logger): IOpenAICommandService
{     
    static readonly JsonSerializerOptions JsonOpts = new()
         {
             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
             DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
             Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
         };
    public async Task<ApiResponse<JobGenResponse>> GenerateJobAsync(string companyId, JobGenRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var service = configuration.GetValue<string>("ai-source") ?? "ai-service-v2";
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Post,
                appId: service,
                methodName: $"drafts/{companyId}/generate"
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

            if (service == "ai-service-v2")
            {
                var result = JsonSerializer.Deserialize<ApiResponse<JobGenResponse>>(raw, JsonOpts);

                return result ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service.");
            }
            else
            {
                var result = JsonSerializer.Deserialize<JobGenResponse>(raw, JsonOpts);
                return new ApiResponse<JobGenResponse>()
                    { Data = result, Success = true, StatusCode = HttpStatusCode.OK };
            }

           
        }catch (Exception e)
        {
            _logger.LogError(e, "Error generating job draft");
            return new ApiResponse<JobGenResponse>() { Success = false, StatusCode = HttpStatusCode.InternalServerError, Exceptions = new ApiError()
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