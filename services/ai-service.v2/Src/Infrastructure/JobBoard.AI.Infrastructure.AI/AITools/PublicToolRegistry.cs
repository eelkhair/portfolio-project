using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Infrastructure.AI.AITools.Public;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools;

public class PublicToolRegistry(
    IAiToolHandlerResolver toolResolver,
    IActivityFactory activityFactory,
    IMonolithApiClient monolithClient,
    IAiDbContext dbContext,
    ILogger<PublicToolRegistry> logger
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return FindMatchingJobsTool.Get(activityFactory, toolResolver, dbContext, monolithClient, logger);
        yield return SearchJobsTool.Get(activityFactory, toolResolver, monolithClient, logger);
        yield return GetSimilarJobsTool.Get(activityFactory, toolResolver, monolithClient, logger);
        yield return GetJobDetailTool.Get(activityFactory, monolithClient, logger);
    }
}
