using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.Services;

public record ConversationDto
{
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = null!;
    public List<ChatMessage> Messages { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
    public string LastTraceId { get; set; } = null!;
    public List<string>TraceParents { get; set; } = [];
}
public interface IConversationStore
{
    Task<List<ChatMessage>> GetChatMessages(Guid conversationId, string userId);
    Task SaveChatMessages(Guid conversationId, string userId, List<ChatMessage> messages);
}

public class ConversationStore(IRedisStore store) : IConversationStore
{
    public async Task<List<ChatMessage>> GetChatMessages(Guid conversationId, string userId)
    {
        var key = $"conversations:{userId}:{conversationId}";
        var result =  await store.GetAsync<ConversationDto>(key, 2);

        if (result == null) return [];
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
        
        return result.Messages ?? [];
    }

    public async Task SaveChatMessages(Guid conversationId, string userId, List<ChatMessage> messages)
    {
        var key = $"conversations:{userId}:{conversationId}";
        var result =  await store.GetAsync<ConversationDto>(key, 2);
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
                    .Where(tp =>  tp != traceParent)
                    .TakeLast(15) 
            );
        }
        var dto = new ConversationDto
        {
            ConversationId = conversationId,
            UserId = userId,
            Messages = messages,
            UpdatedAt = DateTime.UtcNow,
            TraceParents = traceParents,
            LastTraceId = Activity.Current?.TraceId.ToString() ?? string.Empty 
        };
        await store.SetAsync(key, dto, 2, TimeSpan.FromDays(2));
    }
}