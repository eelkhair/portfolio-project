using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Shared;

public static class ListIndustriesTool
{
    public static AIFunction Get<T>(
        IActivityFactory activityFactory,
        Func<CancellationToken, Task<List<T>>> fetchIndustries,
        string source,
        IMemoryCache cache,
        IConversationContext conversation,
        TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
                await ToolHelper.ExecuteCachedAsync(
                    activityFactory, 
                    "industry_list", 
                    cache,
                    $"industry_list:{conversation.ConversationId}",
                    toolTtl, 
                    fetchIndustries, 
                    list => list.Count,
                    ToolHelper.Tags(
                        ("tool.source", source)
                    ), ct),
            new AIFunctionFactoryOptions
            {
                Name = "industry_list",
                Description = "Returns a list all industries in the system."
            });
    }
}
