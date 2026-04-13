using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using AdminAPI.Contracts.Services;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands;

public partial class OpenAICommandService(DaprClient client, UserContextService accessor, ILogger<OpenAICommandService> logger) : IOpenAICommandService
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
            LogGeneratingJob(logger, companyId);
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Post,
                appId: "ai-service-v2",
                methodName: $"drafts/{companyId}/generate"
            );

            if (accessor.GetHeader("Authorization") is { } auth && !string.IsNullOrWhiteSpace(auth))
                req.Headers.TryAddWithoutValidation("Authorization", auth);


            req.Content = JsonContent.Create(request, options: JsonOpts);

            using var resp = await client.InvokeMethodWithResponseAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                LogAiServiceError(logger, (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service-v2 {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ApiResponse<JobGenResponse>>(raw, JsonOpts);
            LogJobGenerated(logger, companyId);

            return result ?? throw new InvalidOperationException("Empty or invalid JSON from ai-service-v2.");
        }
        catch (Exception e)
        {
            LogGenerateJobError(logger, e, companyId);
            return new ApiResponse<JobGenResponse>()
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Exceptions = new ApiError()
                {
                    Message = e.Message,
                    Errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    {"Error", [e.Message]}
                }
                }
            };
        }
    }

    [LoggerMessage(LogLevel.Information, "Generating job draft for company {CompanyId}")]
    static partial void LogGeneratingJob(ILogger logger, string companyId);

    [LoggerMessage(LogLevel.Information, "Job draft generated for company {CompanyId}")]
    static partial void LogJobGenerated(ILogger logger, string companyId);

    [LoggerMessage(LogLevel.Error, "ai-service-v2 returned {StatusCode}: {Body}")]
    static partial void LogAiServiceError(ILogger logger, int statusCode, string body);

    [LoggerMessage(LogLevel.Error, "Error generating job draft for company {CompanyId}")]
    static partial void LogGenerateJobError(ILogger logger, Exception exception, string companyId);
}
