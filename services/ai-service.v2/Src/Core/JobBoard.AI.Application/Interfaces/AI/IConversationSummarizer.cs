using Microsoft.Extensions.AI;

namespace JobBoard.AI.Application.Interfaces.AI;

public interface IConversationSummarizer
{
    /// <summary>
    /// Compresses older conversation messages into a concise summary.
    /// If an existing summary is provided, merges it with new messages.
    /// </summary>
    Task<string> SummarizeAsync(
        string? existingSummary,
        List<ChatMessage> messagesToSummarize,
        CancellationToken cancellationToken);
}
