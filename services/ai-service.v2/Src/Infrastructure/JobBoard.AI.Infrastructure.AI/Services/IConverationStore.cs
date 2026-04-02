using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace JobBoard.AI.Infrastructure.AI.Services;

public record ConversationDto
{
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = null!;
    public List<ChatMessage> Messages { get; set; } = [];
    public string? Summary { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string LastTraceId { get; set; } = null!;
    public List<string> TraceParents { get; set; } = [];
}

public record ConversationSnapshot(List<ChatMessage> Messages, string? Summary);

public interface IConversationStore
{
    Task<ConversationSnapshot> GetConversation(Guid conversationId, string userId);
    Task SaveConversation(Guid conversationId, string userId, List<ChatMessage> messages, string? summary);
    int AiDbId { get; }
}

public class ConversationStore(IRedisStore store, IConfiguration configuration) : IConversationStore
{
    public int AiDbId { get; } = int.TryParse(configuration["Redis:AiDb"], out var db) ? db : 2;

    public async Task<ConversationSnapshot> GetConversation(Guid conversationId, string userId)
    {
        var key = $"conversations:{userId}:{conversationId}";
        var result = await store.GetAsync<ConversationDto>(key, AiDbId);

        if (result == null) return new ConversationSnapshot([], null);

        foreach (var traceParent in result.TraceParents)
        {
            if (ActivityContext.TryParse(traceParent, null, out var parentContext))
            {
                Activity.Current?.AddLink(
                    new ActivityLink(
                        parentContext,
                        new ActivityTagsCollection
                        {
                            ["link.type"] = "conversation-continuation",
                            ["conversation.id"] = conversationId.ToString()
                        }
                    )
                );
            }
        }

        return new ConversationSnapshot(result.Messages ?? [], result.Summary);
    }

    public async Task SaveConversation(Guid conversationId, string userId, List<ChatMessage> messages,
        string? summary)
    {
        var key = $"conversations:{userId}:{conversationId}";
        var result = await store.GetAsync<ConversationDto>(key, AiDbId);
        var traceParents = new List<string>();

        var traceParent = string.Empty;
        if (Activity.Current?.GetTraceParent() is { } current)
        {
            traceParents.Add(current);
            traceParent = current;
        }

        if (result?.TraceParents is { Count: > 0 })
        {
            traceParents.AddRange(
                result.TraceParents
                    .Where(tp => tp != traceParent)
                    .TakeLast(15)
            );
        }

        var dto = new ConversationDto
        {
            ConversationId = conversationId,
            UserId = userId,
            Messages = messages,
            Summary = summary,
            UpdatedAt = DateTime.UtcNow,
            TraceParents = traceParents,
            LastTraceId = Activity.Current?.TraceId.ToString() ?? string.Empty
        };
        await store.SetAsync(key, dto, AiDbId, TimeSpan.FromDays(2));
    }
}