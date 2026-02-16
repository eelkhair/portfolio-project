using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Shared;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith;

public class MonolithToolRegistry(IMonolithApiClient client,
    IActivityFactory activityFactory,
    IMemoryCache cache,
    IConversationContext conversation) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromMinutes(5);

    public IEnumerable<AITool> GetTools()
    {
        yield return ListCompaniesTool.Get(activityFactory,
            async ct => (await client.ListCompaniesAsync(ct)).Value,
            "monolith", cache, conversation, ToolTtl);
        yield return ListIndustriesTool.Get(activityFactory,
            async ct => (await client.ListIndustriesAsync(ct)).Value,
            "monolith", cache, conversation, ToolTtl);
        yield return CreateCompanyTool.Get<CreateCompanyCommand>(activityFactory,
            async (cmd, ct) => await client.CreateCompanyAsync(cmd, ct), "monolith");
    }
}
