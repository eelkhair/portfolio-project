using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.AITools.Admins.System;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins;

public class AdminToolRegistry(
    IAiToolHandlerResolver toolResolver,
    IActivityFactory activityFactory,
    IRedisStore redisStore,
    IUserAccessor userAccessor,
    ILogger<AdminToolRegistry> logger,
    IConversationContext conversationContext,
    IConversationStore conversationStore
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return TraceIdTool.Get(activityFactory, redisStore, userAccessor, conversationContext, conversationStore);
        yield return ConversationIdTool.Get(activityFactory, conversationContext);
        yield return ProviderRetrievalTool.Get(activityFactory, toolResolver, logger);
    }
}
