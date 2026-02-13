using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;

namespace JobBoard.AI.Application.Actions.Chat;

public sealed class ChatCommand(
    string message,
    Guid? companyId
) : BaseCommand<string>
{
    public string Message { get; } = message;
    public Guid? CompanyId { get; } = companyId;
}

public sealed class ChatCommandHandler(
    IHandlerContext context,
    IChatSystemPrompt systemPrompt,
    ICompletionService completionService,
    IActivityFactory activityFactory
) : BaseCommandHandler(context),
    IHandler<ChatCommand, string>
{
    public async Task<string> HandleAsync(
        ChatCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "ChatCommand",
            ActivityKind.Internal);

        activity?.SetTag("chat.message", request.Message);
        activity?.SetTag("chat.companyId", request.CompanyId);

        var effectiveUserMessage = request.CompanyId is not null
            ? $"Context:\n- companyId: {request.CompanyId}\n\nUser:\n{request.Message}"
            : request.Message;
        return await completionService.RunChatAsync(
            systemPrompt.Value,
            effectiveUserMessage,
            cancellationToken);
    }
}
