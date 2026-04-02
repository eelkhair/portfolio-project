using System.Text.Json.Serialization;

namespace JobBoard.AI.Application.Actions.Chat;

public class ChatResponse
{
    public required string Response { get; set; }
    public Guid ConversationId { get; set; }
    public string TraceId { get; set; }

    /// <summary>
    /// Structured tool results returned directly without a second LLM call.
    /// Null when the LLM generated a text response.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolData>? ToolResults { get; set; }
}

public class ToolData
{
    public required string Tool { get; set; }
    public required object Result { get; set; }
}
