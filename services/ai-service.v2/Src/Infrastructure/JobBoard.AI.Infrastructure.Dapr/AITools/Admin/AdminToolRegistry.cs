using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Companies;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Industries;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Jobs;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin;

public class AdminToolRegistry(IAdminApiClient client,
    IActivityFactory activityFactory,
    IMemoryCache cache,
    IConversationContext conversation,
    IAiDbContext dbContext,
    ILoggerFactory loggerFactory
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return ListCompaniesTool.Get(activityFactory, client, cache, conversation, TimeSpan.FromMinutes(5));
        yield return ListIndustriesTool.Get(activityFactory, client, cache, conversation, TimeSpan.FromMinutes(5));
        yield return CreateCompanyTool.Get(activityFactory, client);
        yield return CreateJobTool.Get(activityFactory, client, dbContext, loggerFactory);
    }
}
