using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Public.Micro;

public class PublicMicroToolRegistry : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        return [];
    }
}