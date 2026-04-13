using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Infrastructure.AI.AITools.Admins.System;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.AspNetCore.Http;
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
    IConversationStore conversationStore,
    IDraftPersistence draftPersistence,
    IAiNotificationHub notificationHub,
    ISettingsService settingsService,
    IHttpContextAccessor httpContextAccessor
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return SystemInfoTool.Get(activityFactory, toolResolver, redisStore, userAccessor,
            conversationContext, conversationStore, settingsService, httpContextAccessor, logger);
        yield return GenerateDraftTool.Get(activityFactory, toolResolver, draftPersistence, notificationHub, userAccessor, logger);
    }
}
