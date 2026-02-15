using System.Diagnostics;
using System.Runtime.CompilerServices;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;

namespace JobBoard.AI.Application.Actions.Chat;

public sealed class ChatStreamCommand(
    string message,
    Guid? companyId,
    Guid? conversationId
) : BaseCommand<IAsyncEnumerable<string>>, IConversationContext
{
    public string Message { get; } = message;
    public Guid? CompanyId { get; } = companyId;
    public Guid? ConversationId { get; set; } = conversationId;
}

public sealed class ChatStreamCommandHandler(
    IHandlerContext context,
    IChatSystemPrompt systemPrompt,
    ICompletionService completionService,
    IActivityFactory activityFactory
) : BaseCommandHandler(context),
    IHandler<ChatStreamCommand, IAsyncEnumerable<string>>
{
    public Task<IAsyncEnumerable<string>> HandleAsync(
        ChatStreamCommand request,
        CancellationToken cancellationToken)
    {
        var effectiveUserMessage = request.CompanyId is not null
            ? $"Context:\n- companyId: {request.CompanyId}\n\nUser:\n{request.Message}"
            : request.Message;

        return Task.FromResult(
            StreamWithTrace(request, effectiveUserMessage, cancellationToken));
    }

    private async IAsyncEnumerable<string> StreamWithTrace(
        ChatStreamCommand request,
        string effectiveUserMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "ChatStreamCommand",
            ActivityKind.Internal);

        activity?.SetTag("chat.message", request.Message);
        activity?.SetTag("chat.companyId", request.CompanyId?.ToString());
        activity?.SetTag("ai.operation", "chat.stream");
        activity?.SetTag("ai.userId", request.UserId);

        var chunkCount = 0;

        await foreach (var chunk in completionService.StreamChatAsync(
                           systemPrompt.Value,
                           effectiveUserMessage,
                           cancellationToken))
        {
            chunkCount++;
            yield return chunk;
        }

        activity?.SetTag("ai.stream.chunk_count", chunkCount);
    }
}
