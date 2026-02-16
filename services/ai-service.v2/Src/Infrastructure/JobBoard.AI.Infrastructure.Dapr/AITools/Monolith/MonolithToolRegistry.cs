using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Industries;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith;

public class MonolithToolRegistry(IMonolithApiClient client,
    IActivityFactory activityFactory,
    IToolExecutionCache cache,
    IUserAccessor accessor) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return ListCompaniesTool.Get(activityFactory, client, cache, accessor, TimeSpan.FromMinutes(5));
        yield return ListIndustriesTool.Get(activityFactory, client, cache, accessor, TimeSpan.FromMinutes(5));
        yield return CreateCompanyTool.Get(activityFactory, client);
    }
}