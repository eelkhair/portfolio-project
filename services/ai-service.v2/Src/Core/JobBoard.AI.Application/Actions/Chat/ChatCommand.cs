using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;

namespace JobBoard.AI.Application.Actions.Chat;

public sealed class ChatCommand(
    string message,
    Guid? companyId,
    Guid? conversationId
) : BaseCommand<ChatResponse>, IConversationContext
{
    public string Message { get; } = message;
    public Guid? CompanyId { get; } = companyId;
    public Guid? ConversationId { get; set; } = conversationId;
}

public sealed class ChatCommandHandler(
    IHandlerContext context,
    IChatSystemPrompt systemPrompt,
    ICompletionService completionService,
    IActivityFactory activityFactory
) : BaseCommandHandler(context),
    IHandler<ChatCommand, ChatResponse>
{
    public async Task<ChatResponse> HandleAsync(
        ChatCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "ChatCommand",
            ActivityKind.Internal);

    
        activity?.SetTag("chat.message", request.Message);
        activity?.SetTag("chat.companyId", request.CompanyId);
        activity?.SetTag("ai.operation", "chat");
        activity?.SetTag("ai.userId", request.UserId);

        var effectiveUserMessage = request.CompanyId is not null
            ? $"Context:\n- companyId: {request.CompanyId}\n\nUser:\n{request.Message}"
            : request.Message;
        
        
        return await completionService.RunChatAsync(
            systemPrompt.Value,
            effectiveUserMessage,
            cancellationToken);
    }
}
