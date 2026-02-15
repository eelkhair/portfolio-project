using JobBoard.AI.Application.Actions.Chat;

namespace JobBoard.AI.Application.Interfaces.AI;

public interface ICompletionService
{
    Task<T> GetResponseAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
    Task<ChatResponse> RunChatAsync(string systemPrompt, string userMessage,
        CancellationToken cancellationToken);
}