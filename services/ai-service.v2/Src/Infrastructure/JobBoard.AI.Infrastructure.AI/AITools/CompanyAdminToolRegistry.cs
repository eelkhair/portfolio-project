using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools;

public class CompanyAdminToolRegistry : IAiTools
{
    public IEnumerable<AITool> GetTools() => [];
}
