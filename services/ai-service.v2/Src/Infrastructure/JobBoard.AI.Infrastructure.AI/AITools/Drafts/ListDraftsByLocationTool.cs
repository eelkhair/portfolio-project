using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Infrastructure.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class ListDraftsByLocationTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IToolExecutionCache cache, IUserAccessor userAccessor, TimeSpan toolTtl)
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
                var cacheKey = $"draft_list:{userAccessor.UserId}:{companyId}:{location}";

                if (cache.TryGet(cacheKey, out var cachedObj))
                {
                    var entry = (ToolCacheEntry)cachedObj!;
                    var age = DateTimeOffset.UtcNow - entry.ExecutedAt;

                    if (age < toolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes);
                        return (ToolResultEnvelope<List<DraftResponse>>)entry.Value;


                    }

                    activity?.SetTag("tool.cache.expired", true);
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

                cache.Set(
                    cacheKey,
                    new ToolCacheEntry(envelope, envelope.ExecutedAt));

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