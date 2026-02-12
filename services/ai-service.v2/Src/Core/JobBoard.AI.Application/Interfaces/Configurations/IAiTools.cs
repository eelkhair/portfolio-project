using Microsoft.Extensions.AI;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IAiTools
{
    IEnumerable<AITool> GetTools();
}