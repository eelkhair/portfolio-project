namespace JobBoard.AI.Application.Interfaces.AI;

/// <summary>
/// Selects which tool groups to include based on the user's message and conversation context.
/// Returns group names (e.g. "company", "draft", "job") that determine which tools are sent to the LLM.
/// </summary>
public interface IToolGroupSelector
{
    /// <summary>
    /// Returns the set of tool group names relevant to the current message.
    /// Always includes "core". Falls back to all groups if no keywords match.
    /// </summary>
    HashSet<string> SelectGroups(string userMessage, string? conversationSummary);
}
