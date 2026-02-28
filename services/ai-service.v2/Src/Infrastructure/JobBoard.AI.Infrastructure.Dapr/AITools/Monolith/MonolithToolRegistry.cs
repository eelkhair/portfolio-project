using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Industries;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Jobs;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith;

public class MonolithToolRegistry(IMonolithApiClient client,
    IActivityFactory activityFactory,
    IMemoryCache cache,
    IConversationContext conversation,
    IAiDbContext dbContext,
    ILoggerFactory loggerFactory,
    IAiNotificationHub notificationHub,
    IUserAccessor userAccessor) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return ListCompaniesTool.Get(activityFactory, client, cache, conversation, TimeSpan.FromMinutes(5));
        yield return ListIndustriesTool.Get(activityFactory, client, cache, conversation, TimeSpan.FromMinutes(5));
        yield return CreateCompanyTool.Get(activityFactory, client);
        yield return CreateJobTool.Get(activityFactory, client, dbContext, loggerFactory, notificationHub, userAccessor);
        yield return ListJobsTool.Get(activityFactory, client, cache, conversation, TimeSpan.FromMinutes(5));
    }
}
