using AdminAPI.Contracts.Models.Companies.Requests;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Shared;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin;

public class AdminToolRegistry(IAdminApiClient client,
    IActivityFactory activityFactory,
    IMemoryCache cache,
    IConversationContext conversation
) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromMinutes(5);

    public IEnumerable<AITool> GetTools()
    {
        yield return ListCompaniesTool.Get(activityFactory,
            async ct => (await client.ListCompaniesAsync(ct)).Data!,
            "microservices", cache, conversation, ToolTtl);
        yield return ListIndustriesTool.Get(activityFactory,
            async ct => (await client.ListIndustriesAsync(ct)).Data!,
            "microservices", cache, conversation, ToolTtl);
        yield return CreateCompanyTool.Get<CreateCompanyRequest>(activityFactory,
            async (cmd, ct) => (object)(await client.CreateCompanyAsync(cmd, ct))!);
    }
}
