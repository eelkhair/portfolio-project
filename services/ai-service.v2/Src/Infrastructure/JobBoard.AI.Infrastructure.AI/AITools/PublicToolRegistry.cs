using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools;

public class PublicToolRegistry : IAiTools
{
    public IEnumerable<AITool> GetTools() => [];
}
