using System.Diagnostics;
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
        return AIFunctionFactory.Create((CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.conversation_id",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "conversation_id");

                
                var conversationId = conversationContext.ConversationId;

                activity?.SetTag("conversation.id", conversationId);
                return Task.FromResult(conversationId);
                
            },
            new AIFunctionFactoryOptions
            {
                Name = "conversation_id",
                Description =
                    "Returns the conversation id for the current conversation."
            });
    }
}