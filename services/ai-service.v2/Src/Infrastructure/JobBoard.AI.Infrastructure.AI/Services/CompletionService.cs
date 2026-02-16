using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponse = JobBoard.AI.Application.Actions.Chat.ChatResponse;

namespace JobBoard.AI.Infrastructure.AI.Services;

public class CompletionService(
    IActivityFactory activityFactory,
    IChatOptionsFactory chatOptionsFactory,
    IConversationStore conversationStore,
    IConversationContext conversationContext,
    IUserAccessor userAccessor,
    IConfiguration configuration,
    IServiceProvider serviceProvider)
    : ICompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    
    public async Task<ChatResponse> RunChatAsync(
        string systemPrompt,
        string userMessage,
        bool allowTools,
        CancellationToken cancellationToken)
    {
        var client = new FunctionInvokingChatClient(GetClient());

        var messages = new List<ChatMessage>()
        {
            new(ChatRole.System, systemPrompt)
        };
        
        var savedMessages = await conversationStore.GetChatMessages(conversationContext.ConversationId!.Value, userAccessor.UserId!);
        messages.AddRange(savedMessages);
        
        var options = chatOptionsFactory.Create(allowTools);
        
        messages.Add(new(ChatRole.User, userMessage));

        var response = await client.GetResponseAsync(
            messages,
            options,
            cancellationToken: cancellationToken);

        Activity.Current?.SetTag("ai.response.length", response.Text.Length);
        Activity.Current?.SetTag("ai.tokens.total", response.Usage?.TotalTokenCount ?? 0);
        Activity.Current?.SetTag("ai.tokens.input", response.Usage?.InputTokenCount ?? 0);
        Activity.Current?.SetTag("ai.tokens.output", response.Usage?.OutputTokenCount ?? 0);
        Activity.Current?.SetTag("ai.conversationId", conversationContext.ConversationId);

        messages.AddMessages(response);
        messages = messages.TakeLast(40).ToList();
        await conversationStore.SaveChatMessages(
            conversationContext.ConversationId.Value,
            userAccessor.UserId!,
            messages.Where(m => m.Role != ChatRole.System).ToList());
         
        return new ChatResponse
        {
            Response = response.Text,
            ConversationId = conversationContext.ConversationId.Value,
            TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty,
        };
    }

    public async Task<T> GetResponseAsync<T>(string systemPrompt, string userPrompt, bool allowTools,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        var messages = new []
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        };
        try
        {
            var chatOptions = chatOptionsFactory.Create(allowTools);

            var response = await client.GetResponseAsync(
                messages, chatOptions,
                cancellationToken: cancellationToken);


            Activity.Current?.SetTag("ai.response.length", response.Text.Length);
            Activity.Current?.SetTag("ai.tokens.total", response.Usage?.TotalTokenCount ?? 0);
            Activity.Current?.SetTag("ai.tokens.input", response.Usage?.InputTokenCount ?? 0);
            Activity.Current?.SetTag("ai.tokens.output", response.Usage?.OutputTokenCount ?? 0);

            Activity.Current?.SetTag("ai.response.reason", response.FinishReason);
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
    
    private IChatClient GetClient()
    {
        using var activity = activityFactory.StartActivity(
            "ai.completion",
            ActivityKind.Internal);
        var provider = configuration["AIProvider"]?.ToLowerInvariant()
                       ?? throw new InvalidOperationException("AI Provider not configured");
        
        return serviceProvider.GetRequiredKeyedService<IChatClient>(provider);
    }
    
    private static string NormalizeJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("AI response was empty.");

        text = text.Trim();

        // Remove markdown code fences if present
        if (!text.StartsWith("```")) return text;
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return text.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return text;
    }
}