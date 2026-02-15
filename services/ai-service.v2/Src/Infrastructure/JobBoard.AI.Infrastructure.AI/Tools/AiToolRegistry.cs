using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.Tools.Drafts;
using JobBoard.AI.Infrastructure.AI.Tools.System;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.Tools;

public class AiToolRegistry(
    IAiToolHandlerResolver toolResolver,
    IActivityFactory activityFactory,
    IRedisStore redisStore,
    IUserAccessor userAccessor,
    IToolExecutionCache cache,
    IConversationContext conversationContext
) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromHours(1);

    public IEnumerable<AITool> GetTools()
    {
        yield return SaveDraftTool.Get(activityFactory, toolResolver);
        yield return ListDraftsTool.Get(activityFactory, toolResolver, cache, ToolTtl);
        yield return ListDraftsByLocationTool.Get(activityFactory, toolResolver, cache, ToolTtl);
        yield return LastTraceIdTool.Get(activityFactory, redisStore,userAccessor, conversationContext);
    }
}
