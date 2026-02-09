using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


namespace JobBoard.AI.Infrastructure.AI.Services;

public class CompletionService(
    IActivityFactory activityFactory,
    IConfiguration configuration,
    IServiceProvider serviceProvider)
    : ICompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    private IChatClient GetClient()
    {
       
        var provider = configuration["AIProvider"]?.ToLowerInvariant()
                       ?? throw new InvalidOperationException("AIProvider not configured");
        Activity.Current?.SetTag("ai.provider", provider);
        return serviceProvider.GetRequiredKeyedService<IChatClient>(provider);
    }

    public async Task<T> GetResponseAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var provider = configuration["AIProvider"]?.ToLowerInvariant()
                       ?? throw new InvalidOperationException("AIProvider not configured");
        var client = GetClient();
        var messages = new []
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        };
        try
        {
            var response = await client.GetResponseAsync(
                messages, serviceProvider.GetRequiredService<ChatOptions>(),
                cancellationToken: cancellationToken);


            Activity.Current?.SetTag("ai.response.length", response.Text.Length);
            Activity.Current?.SetTag("ai.tokens.total", response.Usage?.TotalTokenCount ?? 0);
            Activity.Current?.SetTag("ai.tokens.input", response.Usage?.InputTokenCount ?? 0);
            Activity.Current?.SetTag("ai.tokens.output", response.Usage?.OutputTokenCount ?? 0);


            return JsonSerializer.Deserialize<T>(
                       
                            NormalizeJson(response.Text)
                          , JsonOptions) ??
                   throw new InvalidOperationException("ai-service returned empty JSON payload.");

        }
        catch (Exception ex)
        {
            Activity.Current?.SetTag("ai.error", true);
            Activity.Current?.SetTag("ai.error.message", ex.Message);
            throw;
        }
    }
    
    private static string NormalizeJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("AI response was empty.");

        text = text.Trim();

        // Remove markdown code fences if present
        if (text.StartsWith("```"))
        {
            var firstBrace = text.IndexOf('{');
            var lastBrace = text.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return text.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
        }

        return text;
    }
}