using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Extensions.Logging;

namespace JobBoard.infrastructure.Dapr;

public class AiServiceClient(DaprClient client, IUserAccessor accessor, ILogger<AiServiceClient> logger)
    : IAiServiceClient
{ static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<List<JobDraftResponse>> ListDrafts(Guid companyId, CancellationToken cancellationToken)
    {
        try
        {
            var req = client.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId: "ai-service",
                methodName: $"drafts/{companyId}"
            );

            req.Headers.TryAddWithoutValidation("Authorization", accessor.Token);


            using var resp = await client.InvokeMethodWithResponseAsync(req, cancellationToken);

            var raw = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {

                logger.LogError("ai-service returned {StatusCode}: {Body}", (int)resp.StatusCode, raw);

                throw new HttpRequestException(
                    $"ai-service {resp.StatusCode}: {raw}", null, resp.StatusCode);
            }

            var result = JsonSerializer.Deserialize<List<JobDraftResponse>>(raw, JsonOpts);

            if (result is null)
                throw new InvalidOperationException("Empty or invalid JSON from ai-service.");

            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error generating job draft");
            throw;
        }
    }
}