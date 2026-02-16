using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class ListDraftsTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
                await ToolHelper.ExecuteCachedAsync(
                    activityFactory, "draft_list", cache,
                    $"draft_list:{conversation.ConversationId}:{companyId}",
                    toolTtl,
                    async token =>
                    {
                        var handler = toolResolver.Resolve<ListDraftsQuery, List<DraftResponse>>();
                        return await handler.HandleAsync(new ListDraftsQuery(companyId), token);
                    },
                    list => list.Count, ct,
                    ("tool.company_id", companyId)),
            new AIFunctionFactoryOptions
            {
                Name = "draft_list",
                Description =
                    """
                    "Returns a list of drafts for a company.
                    """
            });
    }
}
