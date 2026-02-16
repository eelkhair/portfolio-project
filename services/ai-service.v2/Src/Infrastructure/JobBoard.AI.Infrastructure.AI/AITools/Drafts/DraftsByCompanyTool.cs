using JobBoard.AI.Application.Actions.Drafts.DraftsByCompany;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class DraftsByCompanyTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (CancellationToken ct) =>
                await ToolHelper.ExecuteCachedAsync(
                    activityFactory, 
                    "drafts_by_company", 
                    cache,
                    $"drafts_by_company:{conversation.ConversationId}",
                    toolTtl,
                    async token =>
                    {
                        var handler = toolResolver.Resolve<DraftsByCompanyQuery, DraftsByCompanyResponse>();
                        return await handler.HandleAsync(new DraftsByCompanyQuery(), token);
                    },
                    r => r.DraftsByCompany.Count, ct),
            new AIFunctionFactoryOptions
            {
                Name = "drafts_by_company",
                Description = """
                              Returns job drafts grouped by company.
                              Each company entry contains a list of drafts and a count.
                              """
            });
    }
}
