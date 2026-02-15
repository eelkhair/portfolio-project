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
        conversationContext.ConversationId = request.ConversationId ?? Guid.NewGuid();
        return await decorated.HandleAsync(request, cancellationToken);
    }
}