namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IConversationContext
{
    public Guid? ConversationId { get; set; }
}

public class ConversationContext : IConversationContext
{
    public Guid? ConversationId { get; set; }
}
