using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools;

public class AdminTools(IAdminApiClient client) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield break;
    }
}