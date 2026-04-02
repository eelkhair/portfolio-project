using System.Diagnostics;
using System.Text;
using JobBoard.AI.Application.Interfaces.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.Services;

public class ConversationSummarizer(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IActivityFactory activityFactory,
    ILogger<ConversationSummarizer> logger)
    : IConversationSummarizer
{
    private const string SystemPrompt = """
                                        Compress the following conversation into a concise summary (max 300 words).
                                        Preserve: key facts, decisions made, entity names/IDs referenced, tool results and their outcomes, any pending or in-progress operations.
                                        Omit: pleasantries, verbose tool JSON payloads, repeated instructions, conversational filler.
                                        If an existing summary is provided, merge it with the new messages into a single unified summary.
                                        Output ONLY the summary text, no preamble.
                                        """;

    public async Task<string> SummarizeAsync(
        string? existingSummary,
        List<ChatMessage> messagesToSummarize,
        CancellationToken cancellationToken)
    {
        if (messagesToSummarize.Count == 0)
            return existingSummary ?? string.Empty;

        using var activity = activityFactory.StartActivity("ai.conversation.summarize", ActivityKind.Internal);
        activity?.SetTag("ai.summarize.message_count", messagesToSummarize.Count);
        activity?.SetTag("ai.summarize.has_existing", existingSummary is not null);

        var userPrompt = BuildUserPrompt(existingSummary, messagesToSummarize);

        var provider = configuration["AIProvider"]?.ToLowerInvariant()
                       ?? throw new InvalidOperationException("AI Provider not configured");
        var client = serviceProvider.GetRequiredKeyedService<IChatClient>(provider);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = 500,
            Temperature = 0.3f,
            ModelId = configuration["AIModel:Summarizer"] ?? configuration["AIModel"]!
        };

        try
        {
            var response = await client.GetResponseAsync(messages, options, cancellationToken);

            activity?.SetTag("ai.summarize.output_tokens", response.Usage?.OutputTokenCount ?? 0);
            activity?.SetTag("ai.summarize.input_tokens", response.Usage?.InputTokenCount ?? 0);

            logger.LogInformation(
                "Summarized {MessageCount} messages into {TokenCount} output tokens",
                messagesToSummarize.Count,
                response.Usage?.OutputTokenCount ?? 0);

            return response.Text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Conversation summarization failed, falling back to existing summary");
            activity?.SetTag("ai.summarize.error", true);
            // Non-fatal: return existing summary so the conversation continues working
            return existingSummary ?? string.Empty;
        }
    }

    private static string BuildUserPrompt(string? existingSummary, List<ChatMessage> messages)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(existingSummary))
        {
            sb.AppendLine("=== EXISTING SUMMARY ===");
            sb.AppendLine(existingSummary);
            sb.AppendLine();
        }

        sb.AppendLine("=== CONVERSATION TO SUMMARIZE ===");

        foreach (var msg in messages)
        {
            var role = msg.Role == ChatRole.User ? "User"
                : msg.Role == ChatRole.Assistant ? "Assistant"
                : "Tool";

            // For tool results, include a compact representation
            foreach (var content in msg.Contents)
            {
                switch (content)
                {
                    case TextContent text:
                        sb.AppendLine($"{role}: {text.Text}");
                        break;
                    case FunctionCallContent call:
                        sb.AppendLine($"Assistant [tool_call]: {call.Name}({TruncateJson(call.Arguments)})");
                        break;
                    case FunctionResultContent result:
                        sb.AppendLine($"Tool [{result.CallId}]: {TruncateText(result.Result?.ToString(), 500)}");
                        break;
                }
            }
        }

        return sb.ToString();
    }

    private static string TruncateJson(IDictionary<string, object?>? args)
    {
        if (args is null || args.Count == 0) return "";
        var pairs = args.Select(kv => $"{kv.Key}={kv.Value}");
        var result = string.Join(", ", pairs);
        return result.Length > 200 ? result[..200] + "..." : result;
    }

    private static string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "(empty)";
        return text.Length > maxLength ? text[..maxLength] + "...(truncated)" : text;
    }
}
