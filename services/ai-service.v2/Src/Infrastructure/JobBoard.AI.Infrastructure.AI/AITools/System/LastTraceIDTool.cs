using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class LastTraceIdTool
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
                    "tool.last_trace",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "last_trace");

                var userId = userAccessor.UserId;
                var conversationId = conversationContext.ConversationId;

                activity?.SetTag("conversation.id", conversationId);
                activity?.SetTag("user.id", userId);

                var convo = await store.GetAsync<ConversationDto>(
                    $"conversations:{userId}:{conversationId}", 2);

                return convo?.LastTraceId;
            },
            new AIFunctionFactoryOptions
            {
                Name = "last_trace",
                Description =
                    "Returns the trace id for the most recent operation in the current conversation."
            });
    }
}