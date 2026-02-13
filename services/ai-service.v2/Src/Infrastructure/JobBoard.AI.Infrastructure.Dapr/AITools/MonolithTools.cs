using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Dapr.AITools.Clients;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools;

public class MonolithTools(IMonolithApiClient client) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield break;
    }
}