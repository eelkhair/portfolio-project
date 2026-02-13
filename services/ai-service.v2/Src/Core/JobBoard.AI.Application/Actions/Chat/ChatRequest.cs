namespace JobBoard.AI.Application.Actions.Chat;

public sealed class ChatRequest
{
    public string Message { get; init; } = default!;
    public Guid? CompanyId { get; init; }
}