using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.AI.AITools.Demo;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools;

public class DemoToolRegistry(
    IActivityFactory activityFactory,
    IMonolithApiClient monolithClient,
    ILogger<DemoToolRegistry> logger
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return ListIndustriesTool.Get(activityFactory, monolithClient, logger);
        yield return CreateDemoCompanyTool.Get(activityFactory, monolithClient, logger);
    }
}
