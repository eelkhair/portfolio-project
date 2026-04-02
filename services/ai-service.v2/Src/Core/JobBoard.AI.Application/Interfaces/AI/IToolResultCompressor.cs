using Microsoft.Extensions.AI;

namespace JobBoard.AI.Application.Interfaces.AI;

/// <summary>
/// Compresses tool result payloads in conversation messages before they are persisted,
/// reducing token costs on subsequent turns.
/// </summary>
public interface IToolResultCompressor
{
    /// <summary>
    /// Walks through messages and replaces large FunctionResultContent payloads
    /// with compact summaries while preserving key data.
    /// </summary>
    void CompressToolResults(List<ChatMessage> messages);
}
