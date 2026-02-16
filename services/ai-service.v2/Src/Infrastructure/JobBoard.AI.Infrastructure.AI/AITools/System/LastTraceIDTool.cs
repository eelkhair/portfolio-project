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
                await ToolHelper.ExecuteAsync(activityFactory, 
                    "last_trace",
                    async (_, token) =>
                    {
                        var userId = userAccessor.UserId;
                        var conversationId = conversationContext.ConversationId;
                        var convo = await store.GetAsync<ConversationDto>(
                            $"conversations:{userId}:{conversationId}", 2);
                        return new { LastTraceId = convo?.LastTraceId, CurrentTraceId = Activity.Current?.TraceId };
                    },
                    ToolHelper.Tags(
                        ("conversation.id", conversationContext.ConversationId), 
                        ("user.id", userAccessor.UserId)), ct),
            new AIFunctionFactoryOptions
            {
                Name = "last_trace",
                Description =
                    "Returns the TraceId of the last message sent to the bot. This tool is useful for debugging and tracking the flow of conversations. It also returns the current TraceId."
            });
    }
}
