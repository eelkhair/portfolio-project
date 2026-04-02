using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins.System;

public static class SystemInfoTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IAiToolHandlerResolver toolResolver,
        IRedisStore store,
        IUserAccessor userAccessor,
        IConversationContext conversationContext,
        IConversationStore conversationStore,
        ISettingsService settingsService,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.system_info",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "system_info");

                var userId = userAccessor.UserId;
                var conversationId = conversationContext.ConversationId;

                // Trace IDs
                var convo = await store.GetAsync<ConversationDto>(
                    $"conversations:{userId}:{conversationId}", conversationStore.AiDbId);

                // Provider info
                var providerQuery = toolResolver.Resolve<GetProviderQuery, GetProviderResponse>();
                var provider = await providerQuery.HandleAsync(new GetProviderQuery(), ct);

                // Application mode
                var mode = await settingsService.GetApplicationModeAsync();

                return new
                {
                    ConversationId = conversationId,
                    JaegerFormat = $"ai.conversationId={conversationId}",
                    LastTraceId = convo?.LastTraceId,
                    CurrentTraceId = Activity.Current?.TraceId.ToString(),
                    Provider = provider.Provider,
                    Model = provider.Model,
                    IsMonolith = mode.IsMonolith
                };
            },
            new AIFunctionFactoryOptions
            {
                Name = "system_info",
                Description =
                    "Returns system state: conversation ID, trace IDs, AI provider/model, and monolith/microservices mode. Use when the user asks about system configuration or debugging info."
            });
    }
}
