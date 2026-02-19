using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class TraceIdTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IRedisStore store,
        IUserAccessor userAccessor,
        IConversationContext conversationContext)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.trace_id",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "trace_id");

                var userId = userAccessor.UserId;
                var conversationId = conversationContext.ConversationId;

                activity?.SetTag("conversation.id", conversationId);
                activity?.SetTag("user.id", userId);

                var convo = await store.GetAsync<ConversationDto>(
                    $"conversations:{userId}:{conversationId}", 2);

                return new {LastTraceId = convo?.LastTraceId, CurrentTraceId = Activity.Current?.TraceId.ToString()};
            },
            new AIFunctionFactoryOptions
            {
                Name = "trace_id",
                Description =
                    """
                    Returns the TraceId of the last message sent to the bot. This tool is useful for debugging and tracking the flow of conversations. It also returns the current TraceId.
                    This tool is useful for debugging and tracking the flow of conversations.
                    It also returns the current TraceId.
                    MUST be returned if the user asks about system configuration
                    """
            });
    }
}