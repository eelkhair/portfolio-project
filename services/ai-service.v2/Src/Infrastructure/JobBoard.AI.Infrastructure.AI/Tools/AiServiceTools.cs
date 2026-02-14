using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Infrastructure.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Tools;

public class AiServiceTools(
    IServiceProvider serviceProvider,
    IActivityFactory activityFactory,
    IToolExecutionCache cache
) : IAiTools
{
    private static readonly TimeSpan ToolTtl = TimeSpan.FromHours(1);

    public IEnumerable<AITool> GetTools()
    {
        yield return SaveDraftAiTool();
        yield return ListDraftsByLocationAiTool();
        yield return ListDraftsAiTool();
        
}
    
    private AIFunction ListDraftsAiTool()
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.draft_list",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "draft_list");
                activity?.AddTag("tool.company_id", companyId);

                var cacheKey = $"draft_list:{companyId}";

                if (cache.TryGet(cacheKey, out var cachedObj))
                {
                    var entry = (ToolCacheEntry)cachedObj!;
                    var age = DateTimeOffset.UtcNow - entry.ExecutedAt;

                    if (age < ToolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes);
                        return (ToolResultEnvelope<List<DraftResponse>>)entry.Value;
                    }

                    activity?.SetTag("tool.cache.expired", true);
                }

                activity?.SetTag("tool.cache.hit", false);

                var handler =
                    serviceProvider.GetRequiredService<
                        IHandler<ListDraftsQuery, List<DraftResponse>>>();

                var drafts = await handler.HandleAsync(
                    new ListDraftsQuery(companyId),
                    ct);

                var envelope = new ToolResultEnvelope<List<DraftResponse>>(
                    drafts,
                    drafts.Count,
                    DateTimeOffset.UtcNow);

                cache.Set(
                    cacheKey,
                    new ToolCacheEntry(envelope, envelope.ExecutedAt));

                activity?.SetTag("tool.result.count", drafts.Count);

                return envelope;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_list",
                Description =
                    """
                    "Returns a list of drafts for a company.
                    """
            });

    }
    private AIFunction ListDraftsByLocationAiTool()
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, string location, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.draft_list_by_location",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "draft_list_by_location");
                activity?.AddTag("tool.company_id", companyId);

                var cacheKey = $"draft_list:{companyId}:{location}";

                if (cache.TryGet(cacheKey, out var cachedObj))
                {
                    var entry = (ToolCacheEntry)cachedObj!;
                    var age = DateTimeOffset.UtcNow - entry.ExecutedAt;

                    if (age < ToolTtl)
                    {
                        activity?.SetTag("tool.cache.hit", true);
                        activity?.SetTag("tool.cache.age_minutes", age.TotalMinutes); 
                        return (ToolResultEnvelope<List<DraftResponse>>)entry.Value;
                        
                       
                    }

                    activity?.SetTag("tool.cache.expired", true);
                }

                activity?.SetTag("tool.cache.hit", false);

                var handler =
                    serviceProvider.GetRequiredService<
                        IHandler<ListDraftsQuery, List<DraftResponse>>>();

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

    }

    private static List<DraftResponse> FilterByLocation(List<DraftResponse> drafts, string location)
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
            .ToList();    }


    private AIFunction SaveDraftAiTool()
    {
        return AIFunctionFactory.Create(
            async (SaveDraftCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.save_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "save_draft");
                activity?.AddTag("tool.company_id", cmd.CompanyId);

                var handler =
                    serviceProvider.GetRequiredService<
                        IHandler<SaveDraftCommand, SaveDraftResponse>>();

                return await handler.HandleAsync(cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company. companyId is required. Ensure CompanyId is populated"
            });
    }
}
