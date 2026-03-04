namespace JobBoard.AI.Application.Actions.Chat;

public sealed class PublicChatRequest
{
    public string Message { get; init; } = default!;
    public Guid? ConversationId { get; init; }
}
