using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Shared;

public static class ListCompaniesTool
{
    public static AIFunction Get<T>(
        IActivityFactory activityFactory,
        Func<CancellationToken, Task<List<T>>> fetchCompanies,
        string source,
        IMemoryCache cache,
        IConversationContext conversation,
        TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
                await ToolHelper.ExecuteCachedAsync(
                    activityFactory, "company_list", cache,
                    $"company_list:{conversation.ConversationId}",
                    toolTtl, fetchCompanies, list => list.Count, ct,
                    ("tool.source", source)),
            new AIFunctionFactoryOptions
            {
                Name = "company_list",
                Description = "Returns a list all companies in the system."
            });
    }
}
