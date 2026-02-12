using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools;

public class AdminTools : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield break;
    }
}