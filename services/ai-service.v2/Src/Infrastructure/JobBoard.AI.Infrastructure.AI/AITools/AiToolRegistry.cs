using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.AITools.Drafts;
using JobBoard.AI.Infrastructure.AI.AITools.System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools;

public class AiToolRegistry(
    IAiToolHandlerResolver toolResolver,
    IActivityFactory activityFactory,
    IRedisStore redisStore,
    IUserAccessor userAccessor,
    IToolExecutionCache cache,
    ILogger<AiToolRegistry> logger,
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
        yield return ConversationIdTool.Get(activityFactory, conversationContext);
        yield return ProviderRetrievalTool.Get(activityFactory, toolResolver, logger);
        yield return IsMonolithTool.Get(activityFactory, redisStore, logger);
        yield return SetModeTool.Get(activityFactory, redisStore, logger);
    }
}
