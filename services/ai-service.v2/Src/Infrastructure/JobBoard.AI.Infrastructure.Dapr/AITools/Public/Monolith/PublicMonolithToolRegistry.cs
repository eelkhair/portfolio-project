using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Public.Monolith;

public class PublicMonolithToolRegistry : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        return [];
    }
}