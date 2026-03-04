using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;

namespace JobBoard.AI.Application.Actions.Chat;

public sealed class ChatCommand(
    string message,
    Guid? companyId,
    Guid? conversationId,
    ChatScope scope = ChatScope.Admin
) : BaseCommand<ChatResponse>, IConversationContext
{
    public string Message { get; } = message;
    public Guid? CompanyId { get; } = companyId;
    public Guid? ConversationId { get; set; } = conversationId;
    public ChatScope Scope { get; } = scope;
}

public sealed class ChatCommandHandler(
    IHandlerContext context,
    IChatSystemPrompt adminSystemPrompt,
    IChatService chatService,
    IActivityFactory activityFactory
) : BaseCommandHandler(context),
    IHandler<ChatCommand, ChatResponse>
{
    private static readonly PublicChatSystemPrompt PublicPrompt = new();

    public async Task<ChatResponse> HandleAsync(
        ChatCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "ChatCommand",
            ActivityKind.Internal);

        activity?.SetTag("chat.message", request.Message);
        activity?.SetTag("chat.companyId", request.CompanyId);
        activity?.SetTag("chat.scope", request.Scope.ToString());
        activity?.SetTag("ai.operation", "chat");
        activity?.SetTag("ai.userId", request.UserId);

        var prompt = request.Scope == ChatScope.Public
            ? PublicPrompt.Value
            : adminSystemPrompt.Value;

        var effectiveUserMessage = request.CompanyId is not null
            ? $"Context:\n- companyId: {request.CompanyId}\n\nUser:\n{request.Message}"
            : request.Message;

        return await chatService.RunChatAsync(
            prompt,
            effectiveUserMessage,
            request.Scope,
            cancellationToken);
    }
}
