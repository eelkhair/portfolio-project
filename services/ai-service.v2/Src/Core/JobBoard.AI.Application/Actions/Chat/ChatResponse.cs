namespace JobBoard.AI.Application.Actions.Chat;

public class ChatResponse
{
    public required string Response { get; set; }
    public Guid ConversationId { get; set; }
    public string TraceId { get; set; }
}