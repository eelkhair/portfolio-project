using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Infrastructure.Decorators;

public class ConversationContextDecorator<ChatCommand, ChatResponse>(
    IHandler<ChatCommand, ChatResponse> decorated,
    IConversationContext conversationContext,
    ILogger<ConversationContextDecorator<ChatCommand, ChatResponse>> logger)
    : IHandler<ChatCommand, ChatResponse> where ChatCommand : IConversationContext, IRequest<ChatResponse>

{
    public async Task<ChatResponse> HandleAsync(ChatCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking conversation id for chat command");
        Activity.Current?.SetTag("new_conversationId",request.ConversationId == null);
        conversationContext.ConversationId = request.ConversationId ?? Guid.NewGuid();
        logger.LogInformation("Handling chat command with conversation id: {ConversationId}", conversationContext.ConversationId);
        Activity.Current?.SetTag("conversationId",conversationContext.ConversationId);
        return await decorated.HandleAsync(request, cancellationToken);
    }
}