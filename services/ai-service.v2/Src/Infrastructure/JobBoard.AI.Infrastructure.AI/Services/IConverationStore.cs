using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.Services;

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
        var results =  await store.GetAsync<List<ChatMessage>>(key, 2);
        return results ?? [];
    }

    public async Task SaveChatMessages(Guid conversationId, string userId, List<ChatMessage> messages)
    {
        var key = $"conversations:{userId}:{conversationId}";
        await store.SetAsync(key, messages, 2, TimeSpan.FromDays(2));
    }
}