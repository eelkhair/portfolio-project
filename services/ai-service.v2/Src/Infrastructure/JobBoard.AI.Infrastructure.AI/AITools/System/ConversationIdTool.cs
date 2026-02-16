using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.System;

public static class ConversationIdTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IConversationContext conversationContext)
    {
        return AIFunctionFactory.Create(() =>
                ToolHelper.ExecuteAsync(activityFactory, "conversation_id",
                    (_, _) => Task.FromResult(conversationContext.ConversationId),
                    CancellationToken.None),
            new AIFunctionFactoryOptions
            {
                Name = "conversation_id",
                Description =
                    "Returns the conversation id for the current conversation." +
                    "Also return in the format 'ai.conversationId={}' for Jaeger" +
                    "Return both versions by default. in multiple lines"
            });
    }
}
