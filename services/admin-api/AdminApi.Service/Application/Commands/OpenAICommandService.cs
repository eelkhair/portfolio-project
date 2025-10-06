using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands;

public class OpenAICommandService(DaprClient client, UserContextService accessor, ILogger<OpenAICommandService> _logger): IOpenAICommandService
{     static readonly JsonSerializerOptions JsonOpts = new()
         {
             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
             DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
             Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
         };
    public async Task<ApiResponse<JobGenResponse>> GenerateJobAsync(string companyId, JobGenRequest request,
        CancellationToken ct = default)
    {
        
   
        var req = client.CreateInvokeMethodRequest(
            HttpMethod.Post,
            appId: "ai-service",
            methodName: $"ai/jobs/{companyId:D}/generate"
        );

        // Forward auth if present
        if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
            req.Headers.TryAddWithoutValidation("Authorization", auth);

        // JSON body with correct casing/enums
        req.Content = JsonContent.Create(request, options: JsonOpts);

        using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

        // Read ONCE as string to avoid stream disposal surprises
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            // Log the body for debugging
            _logger.LogError("ai-service returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

            // Bubble up a clean error (or map to your ApiResponse failure)
            throw new HttpRequestException(
                $"ai-service {resp.StatusCode}: {raw}", null, resp.StatusCode);
        }

        // Now safely deserialize from the string
        var result = JsonSerializer.Deserialize<JobGenResponse>(raw, JsonOpts); 
        
        if (result is null)
            throw new InvalidOperationException("Empty or invalid JSON from ai-service.");

        return new ApiResponse<JobGenResponse>(){Data=result};
    }
}