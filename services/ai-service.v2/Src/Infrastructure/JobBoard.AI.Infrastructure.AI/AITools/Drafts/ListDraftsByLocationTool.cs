using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class ListDraftsByLocationTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IMemoryCache cache, IConversationContext conversation, TimeSpan toolTtl)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, string location, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.draft_list_by_location",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "draft_list_by_location");
                activity?.AddTag("tool.company_id", companyId);
                activity?.AddTag("tool.location", location);
                
                var cacheKey = $"draft_list:{conversation.ConversationId}:{companyId}:{location}";
                activity?.SetTag("tool.cache.key", cacheKey);
                activity?.SetTag("tool.ttl.seconds", toolTtl.TotalSeconds);
                
                if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<List<DraftResponse>>? cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return cached;
                }

                activity?.SetTag("tool.cache.hit", false);

                var handler = toolResolver.Resolve<ListDraftsQuery, List<DraftResponse>>();

                var drafts = await handler.HandleAsync(
                    new ListDraftsQuery(companyId),
                    ct);

                var filteredDrafts = FilterByLocation(drafts, location);

                var envelope = new ToolResultEnvelope<List<DraftResponse>>(
                    filteredDrafts,
                    filteredDrafts.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(cacheKey, envelope, toolTtl);

                activity?.SetTag("tool.result.count", filteredDrafts.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_list_by_location",
                Description =
                    """
                    "Returns the list of drafts by location for a company.
                    State must be normalized to 2 letter code (eg. CA, NY, IA, TX)
                    Remove any drafts that are not in the specified location after the tool is done processing.
                    """
            });

        static List<DraftResponse> FilterByLocation(List<DraftResponse> drafts, string location)
        {
            var parts = location
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            parts[^1] = ", " + parts[^1];
            return drafts
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.Location) &&
                    parts.Any(p =>
                        d.Location.EndsWith(p, StringComparison.OrdinalIgnoreCase) ||
                        d.Location.Contains(p, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

    }
}
