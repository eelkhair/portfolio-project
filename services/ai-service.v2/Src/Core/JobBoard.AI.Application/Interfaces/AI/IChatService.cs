using JobBoard.AI.Application.Actions.Chat;

namespace JobBoard.AI.Application.Interfaces.AI;

public interface IChatService
{
    Task<T> GetResponseAsync<T>(string systemPrompt, string userPrompt, bool allowTools,
        CancellationToken cancellationToken);
    Task<string> GetTextResponseAsync(string systemPrompt, string userPrompt,
        CancellationToken cancellationToken);
    Task<ChatResponse> RunChatAsync(string systemPrompt, string userMessage, ChatScope scope,
        CancellationToken cancellationToken);
}
