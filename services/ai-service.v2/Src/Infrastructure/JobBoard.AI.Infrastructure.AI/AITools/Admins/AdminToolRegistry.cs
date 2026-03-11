using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.AITools.Admins.Drafts;
using JobBoard.AI.Infrastructure.AI.AITools.Admins.System;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins;

public class AdminToolRegistry(
    IAiToolHandlerResolver toolResolver,
    IActivityFactory activityFactory,
    IRedisStore redisStore,
    IUserAccessor userAccessor,
    IMemoryCache cache,
    ISettingsService settingsService,
    ILogger<AdminToolRegistry> logger,
    IAiNotificationHub notificationHub,
    IConversationContext conversationContext,
    IConversationStore conversationStore
) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromMinutes(5);

    public IEnumerable<AITool> GetTools()
    {
        yield return GenerateDraftTool.Get(activityFactory, toolResolver, notificationHub, userAccessor);
        yield return SaveDraftTool.Get(activityFactory, toolResolver);
        yield return ListDraftsTool.Get(activityFactory, toolResolver, cache, conversationContext, ToolTtl);
        yield return ListDraftsByLocationTool.Get(activityFactory, toolResolver, cache, conversationContext, ToolTtl);
        yield return TraceIdTool.Get(activityFactory, redisStore, userAccessor, conversationContext, conversationStore);
        yield return DraftsByCompanyTool.Get(activityFactory, toolResolver, cache, conversationContext, ToolTtl);
        yield return ConversationIdTool.Get(activityFactory, conversationContext);
        yield return ProviderRetrievalTool.Get(activityFactory, toolResolver, logger);
        yield return IsMonolithTool.Get(activityFactory, settingsService, logger);
        yield return SetModeTool.Get(activityFactory, settingsService, logger);
    }
}
