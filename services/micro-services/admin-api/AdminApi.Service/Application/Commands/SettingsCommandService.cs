using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Settings;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands;

public class SettingsCommandService(DaprClient client, UserContextService accessor, ILogger<SettingsCommandService> logger) : ISettingsCommandService
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
                logger.LogError("ai-service-v2 returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);
                throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<GetProviderResponse>(raw, JsonOpts);

            Activity.Current?.SetTag("ai.provider", result?.Provider);
            Activity.Current?.SetTag("ai.model", result?.Model);

            return new ApiResponse<GetProviderResponse>
            {
                Data = result,
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting AI provider settings");
            return new ApiResponse<GetProviderResponse>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
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
                logger.LogError("ai-service-v2 returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);
                throw new HttpRequestException($"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            return new ApiResponse<UpdateProviderResponse>
            {
                Data = new UpdateProviderResponse { Success = true },
                Success = true,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating AI provider settings");
            return new ApiResponse<UpdateProviderResponse>
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Error", [e.Message] }
                    }
                }
            };
        }
    }
}
