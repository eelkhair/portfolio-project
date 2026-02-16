using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Companies;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Industries;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin;

public class AdminToolRegistry(IAdminApiClient client,
    IActivityFactory activityFactory,
    IToolExecutionCache cache,
    IUserAccessor accessor
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return ListCompaniesTool.Get(activityFactory, client, cache, accessor, TimeSpan.FromMinutes(5));
        yield return ListIndustriesTool.Get(activityFactory, client, cache, accessor, TimeSpan.FromMinutes(5));
        yield return CreateCompanyTool.Get(activityFactory, client);
    }
}