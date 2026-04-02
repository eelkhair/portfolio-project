using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.Infrastructure;
using Microsoft.Extensions.AI;
using McpCtx = JobBoard.AI.Infrastructure.AI.Infrastructure.McpRequestContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponse = JobBoard.AI.Application.Actions.Chat.ChatResponse;

namespace JobBoard.AI.Infrastructure.AI.Services;

public class ChatService(
    IActivityFactory activityFactory,
    IChatOptionsFactory chatOptionsFactory,
    IConversationStore conversationStore,
    IConversationContext conversationContext,
    IConversationSummarizer conversationSummarizer,
    IToolResultCompressor toolResultCompressor,
    IUserAccessor userAccessor,
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    IMetricsService metricsService)
    : IChatService
{
    private const int SummarizeThreshold = 20;
    private const int KeepRecentCount = 6;
    private const int MaxToolIterations = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Read-only tools whose results can be returned directly to the UI
    /// without a second LLM call for formatting.
    /// </summary>
    private static readonly HashSet<string> DirectReturnTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "company_list",
        "industry_list",
        "job_list",
        "company_job_summaries",
        "draft_list",
        "drafts_by_company",
        "system_info"
    };

    /// <summary>
    /// User messages matching these patterns indicate list/lookup intent
    /// where direct return is appropriate. Messages that look like questions
    /// about specific entities should go through the LLM for interpretation.
    /// </summary>
    private static bool IsListIntent(string userMessage)
    {
        var msg = userMessage.Trim().ToLowerInvariant();

        // Questions or "tell me about" → need LLM interpretation
        if (msg.StartsWith("tell me") || msg.StartsWith("what") || msg.StartsWith("who") ||
            msg.StartsWith("how") || msg.StartsWith("why") || msg.StartsWith("describe") ||
            msg.StartsWith("explain") || msg.StartsWith("find") || msg.StartsWith("search") ||
            msg.StartsWith("which") || msg.Contains("about") || msg.Contains("detail") ||
            msg.Contains("?"))
        {
            return false;
        }

        // Short imperative commands like "list companies", "show jobs", "get drafts"
        return msg.StartsWith("list") || msg.StartsWith("show") || msg.StartsWith("get") ||
               msg.StartsWith("fetch") || msg.StartsWith("display") ||
               msg.StartsWith("all ") || msg.StartsWith("system info") ||
               msg.StartsWith("sys info") || msg.StartsWith("config");
    }

    public async Task<ChatResponse> RunChatAsync(
        string systemPrompt,
        string userMessage,
        ChatScope scope,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        var provider = configuration["AIProvider"]?.ToLowerInvariant() ?? "unknown";
        var scopeName = scope.ToString();

        var messages = new List<ChatMessage>()
        {
            new(ChatRole.System, systemPrompt)
        };

        var snapshot = await conversationStore.GetConversation(
            conversationContext.ConversationId!.Value, userAccessor.UserId!);

        if (!string.IsNullOrWhiteSpace(snapshot.Summary))
        {
            messages.Add(new(ChatRole.User,
                $"[Previous conversation summary]\n{snapshot.Summary}"));
            messages.Add(new(ChatRole.Assistant,
                "Understood, I have the context from our previous conversation."));
        }

        messages.AddRange(snapshot.Messages);

        var options = chatOptionsFactory.Create(serviceProvider, scope, userMessage, snapshot.Summary);
        var toolMap = BuildToolMap(options.Tools);

        messages.Add(new(ChatRole.User, userMessage));

        metricsService.IncrementChatRequest(scopeName, provider);
        var sw = Stopwatch.StartNew();

        McpCtx.CurrentToken = userAccessor.Token;
        try
        {
            // === Manual tool invocation loop ===
            var totalUsage = new UsageDetails();
            List<ToolData>? directData = null;

            for (var iteration = 0; iteration < MaxToolIterations; iteration++)
            {
                var response = await client.GetResponseAsync(
                    messages, options, cancellationToken: cancellationToken);

                AccumulateUsage(totalUsage, response.Usage);
                messages.AddMessages(response);

                // Extract tool calls from the response
                var toolCalls = response.Messages
                    .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                    .ToList();

                // No tool calls — LLM produced a text response, we're done
                if (toolCalls.Count == 0)
                {
                    RecordMetrics(scopeName, provider, totalUsage, toolCalls, sw);
                    await SaveHistory(messages, snapshot, cancellationToken);

                    return new ChatResponse
                    {
                        Response = response.Text,
                        ConversationId = conversationContext.ConversationId.Value,
                        TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty
                    };
                }

                // Execute tool calls
                var toolResults = await ExecuteToolCallsAsync(toolCalls, toolMap, scopeName, cancellationToken);
                var toolResultMessage = new ChatMessage(ChatRole.Tool, toolResults.Select(r => r.Content).ToList<AIContent>());
                messages.Add(toolResultMessage);

                // Check: are ALL tool calls direct-return AND does the user want a list?
                var allDirect = IsListIntent(userMessage)
                                && toolCalls.All(tc => DirectReturnTools.Contains(tc.Name ?? ""));

                if (allDirect)
                {
                    // Short-circuit: return structured data directly, skip second LLM call
                    directData = toolResults
                        .Select(r => new ToolData
                        {
                            Tool = r.ToolName,
                            Result = ParseJsonOrRaw(r.Content.Result?.ToString())
                        })
                        .ToList();

                    RecordMetrics(scopeName, provider, totalUsage, toolCalls, sw);
                    await SaveHistory(messages, snapshot, cancellationToken);

                    return new ChatResponse
                    {
                        Response = "",
                        ToolResults = directData,
                        ConversationId = conversationContext.ConversationId.Value,
                        TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty
                    };
                }

                // Not all direct — continue the loop, LLM will process tool results
            }

            // Fallback: max iterations reached
            RecordMetrics(scopeName, provider, totalUsage, [], sw);
            await SaveHistory(messages, snapshot, cancellationToken);

            return new ChatResponse
            {
                Response = messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text
                           ?? "The operation could not be completed.",
                ConversationId = conversationContext.ConversationId.Value,
                TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty
            };
        }
        finally
        {
            McpCtx.CurrentToken = null;
        }
    }

    private async Task<List<(string ToolName, FunctionResultContent Content)>> ExecuteToolCallsAsync(
        List<FunctionCallContent> toolCalls,
        Dictionary<string, AITool> toolMap,
        string scopeName,
        CancellationToken ct)
    {
        var results = new List<(string ToolName, FunctionResultContent Content)>();

        foreach (var call in toolCalls)
        {
            metricsService.IncrementToolCall(scopeName, call.Name ?? "unknown");

            if (!toolMap.TryGetValue(call.Name ?? "", out var tool) || tool is not AIFunction function)
            {
                results.Add((call.Name ?? "unknown", new FunctionResultContent(call.CallId,
                    $"Tool '{call.Name}' not found.")));
                continue;
            }

            try
            {
                var args = new AIFunctionArguments(call.Arguments ?? new Dictionary<string, object?>());
                var result = await function.InvokeAsync(args, ct);
                results.Add((call.Name!, new FunctionResultContent(call.CallId, result)));
            }
            catch (Exception ex)
            {
                results.Add((call.Name ?? "unknown", new FunctionResultContent(call.CallId,
                    $"Tool error: {ex.Message}")));
            }
        }

        return results;
    }

    private static Dictionary<string, AITool> BuildToolMap(IList<AITool>? tools)
    {
        if (tools is null || tools.Count == 0) return new();
        return tools.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    }

    private static object ParseJsonOrRaw(string? text)
    {
        if (string.IsNullOrEmpty(text)) return new { };
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(text);
        }
        catch
        {
            return text;
        }
    }

    private static void AccumulateUsage(UsageDetails total, UsageDetails? usage)
    {
        if (usage is null) return;
        total.InputTokenCount = (total.InputTokenCount ?? 0) + (usage.InputTokenCount ?? 0);
        total.OutputTokenCount = (total.OutputTokenCount ?? 0) + (usage.OutputTokenCount ?? 0);
        total.TotalTokenCount = (total.TotalTokenCount ?? 0) + (usage.TotalTokenCount ?? 0);
    }

    private void RecordMetrics(string scopeName, string provider, UsageDetails usage,
        List<FunctionCallContent> toolCalls, Stopwatch sw)
    {
        sw.Stop();
        metricsService.RecordChatRequestDuration(scopeName, provider, sw.Elapsed.TotalMilliseconds);

        Activity.Current?.SetTag("ai.tokens.total", usage.TotalTokenCount ?? 0);
        Activity.Current?.SetTag("ai.tokens.input", usage.InputTokenCount ?? 0);
        Activity.Current?.SetTag("ai.tokens.output", usage.OutputTokenCount ?? 0);
        Activity.Current?.SetTag("ai.conversationId", conversationContext.ConversationId);
        Activity.Current?.SetTag("ai.direct_return", toolCalls.All(tc => DirectReturnTools.Contains(tc.Name ?? "")));

        metricsService.RecordTokenUsage(scopeName, provider,
            usage.InputTokenCount ?? 0,
            usage.OutputTokenCount ?? 0);
    }

    private async Task SaveHistory(List<ChatMessage> messages, ConversationSnapshot snapshot,
        CancellationToken ct)
    {
        // Compress large tool results before persisting
        toolResultCompressor.CompressToolResults(messages);

        messages = messages.TakeLast(40).ToList();
        if (messages.Count > 0 &&
            messages[0].Contents is { Count: > 0 } &&
            messages[0].Contents[0] is FunctionResultContent)
        {
            messages.RemoveAt(0);
        }

        var nonSystemMessages = messages
            .Where(m => m.Role != ChatRole.System)
            .ToList();

        // Remove injected summary context messages before saving
        if (nonSystemMessages.Count >= 2
            && nonSystemMessages[0].Role == ChatRole.User
            && nonSystemMessages[0].Text?.StartsWith("[Previous conversation summary]") == true)
        {
            nonSystemMessages.RemoveRange(0, 2);
        }

        var summary = snapshot.Summary;

        if (nonSystemMessages.Count > SummarizeThreshold)
        {
            var older = nonSystemMessages.SkipLast(KeepRecentCount).ToList();
            var recent = nonSystemMessages.TakeLast(KeepRecentCount).ToList();

            summary = await conversationSummarizer.SummarizeAsync(
                snapshot.Summary, older, ct);

            nonSystemMessages = recent;
        }

        await conversationStore.SaveConversation(
            conversationContext.ConversationId!.Value,
            userAccessor.UserId!,
            nonSystemMessages,
            summary
        );
    }

    public async Task<string> GetTextResponseAsync(string systemPrompt, string userPrompt,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        };
        try
        {
            var chatOptions = chatOptionsFactory.Create(serviceProvider, ChatScope.Public, userPrompt, null);
            var response = await client.GetResponseAsync(
                messages, chatOptions,
                cancellationToken: cancellationToken);

            Activity.Current?.SetTag("ai.response.length", response.Text.Length);
            Activity.Current?.SetTag("ai.tokens.total", response.Usage?.TotalTokenCount ?? 0);
            Activity.Current?.SetTag("ai.tokens.input", response.Usage?.InputTokenCount ?? 0);
            Activity.Current?.SetTag("ai.tokens.output", response.Usage?.OutputTokenCount ?? 0);

            return response.Text;
        }
        catch (Exception ex)
        {
            Activity.Current?.SetTag("ai.error", true);
            Activity.Current?.SetTag("ai.error.message", ex.Message);
            throw;
        }
    }

    public async Task<T> GetResponseAsync<T>(string systemPrompt, string userPrompt, bool allowTools,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        };
        try
        {
            var chatOptions = allowTools
                ? chatOptionsFactory.Create(serviceProvider, ChatScope.Admin, userPrompt, null)
                : new ChatOptions
                {
                    MaxOutputTokens = 5000,
                    Temperature = 0.3f,
                    ModelId = configuration["AIModel"]!
                };

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
