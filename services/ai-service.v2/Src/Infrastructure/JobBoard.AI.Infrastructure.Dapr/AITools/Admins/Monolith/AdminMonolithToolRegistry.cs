using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Companies;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Drafts;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Industries;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Jobs;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith;

public class AdminMonolithToolRegistry(IMonolithApiClient client,
    IActivityFactory activityFactory,
    IMemoryCache cache,
    IConversationContext conversation,
    ILoggerFactory loggerFactory,
    IAiNotificationHub notificationHub,
    IUserAccessor userAccessor) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromMinutes(5);

    public IEnumerable<AITool> GetTools()
    {
        // Company tools
        yield return ListCompaniesTool.Get(activityFactory, client, cache, conversation, ToolTtl);
        yield return ListIndustriesTool.Get(activityFactory, client, cache, conversation, ToolTtl);
        yield return CreateCompanyTool.Get(activityFactory, client);
        yield return UpdateCompanyTool.Get(activityFactory, client);

        // Job tools
        yield return CreateJobTool.Get(activityFactory, client, loggerFactory, notificationHub, userAccessor);
        yield return ListJobsTool.Get(activityFactory, client, cache, conversation, ToolTtl);
        yield return ListCompanyJobSummariesTool.Get(activityFactory, client, cache, conversation, ToolTtl);

        // Draft CRUD tools (persistence in monolith)
        yield return SaveDraftTool.Get(activityFactory, client);
        yield return ListDraftsTool.Get(activityFactory, client, cache, conversation, ToolTtl);
        yield return ListDraftsByLocationTool.Get(activityFactory, client, cache, conversation, ToolTtl);
        yield return DraftsByCompanyTool.Get(activityFactory, client, cache, conversation, ToolTtl);
        yield return DeleteDraftTool.Get(activityFactory, client);
    }
}
