using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.List;
using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Tools;

public class AiServiceTools(IServiceProvider serviceProvider, IActivityFactory activityFactory, IToolExecutionCache cache ) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return AIFunctionFactory.Create(
            async (SaveDraftCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.save_draft", ActivityKind.Internal);
                var handler = serviceProvider.GetRequiredService<IHandler<SaveDraftCommand, SaveDraftResponse>>();
                activity?.AddTag("ai.operation", "save_draft");
                activity?.AddTag("tool.company_id", cmd.CompanyId);
                
                return await handler.HandleAsync(cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company."
            });

        yield return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.draft_list", ActivityKind.Internal);
                activity?.AddTag("tool.company_id", companyId);
                activity?.AddTag("ai.operation", "draft_list");
                
                var cacheKey = $"draft_list:{companyId}";

                if (cache.TryGet(cacheKey, out var cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return (List<DraftResponse>)cached!;
                }
                activity?.SetTag("tool.cache.hit", false);
               
                var handler = serviceProvider.GetRequiredService<IHandler<ListDraftsQuery, List<DraftResponse>>>();
                var response = await handler.HandleAsync(
                    new ListDraftsQuery(companyId),
                    ct);

                cache.Set(cacheKey, response);
                activity?.SetTag("tool.result.count", response.Count);
                return response;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_list",
                Description = "Returns a list of drafts for a company."
            });

        yield return AIFunctionFactory.Create(
            async (Guid companyId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.draft_count", ActivityKind.Internal);
                activity?.AddTag("tool.company_id", companyId);
                activity?.AddTag("ai.operation", "draft_count");
                
                var cacheKey = $"draft_list:{companyId}";
                if (cache.TryGet(cacheKey, out var cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return ((List<DraftResponse>)cached!).Count;
                }
                activity?.SetTag("tool.cache.hit", false);
                
                var handler = serviceProvider.GetRequiredService<IHandler<ListDraftsQuery, List<DraftResponse>>>();
                
                var response = await handler.HandleAsync(
                    new ListDraftsQuery(companyId),
                    ct);

                cache.Set(cacheKey, response);
                activity?.SetTag("tool.result.count", response.Count);

                return response.Count;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_count",
                Description = "Returns count of drafts for a company. Requires companyId."
            });

        
        yield return AIFunctionFactory.Create(
            async (Guid companyId, string location, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.draft_list_by_location", ActivityKind.Internal);
                
                var normalizedLocation = location.Trim().ToUpperInvariant();
                activity?.AddTag("ai.operation", "draft_list_by_location");
                activity?.AddTag("tool.company_id", companyId);
                activity?.AddTag("tool.location", normalizedLocation);
               
                var cacheKey = $"draft_list_by_location:{companyId}:{normalizedLocation}";
                if (cache.TryGet(cacheKey, out var cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return (List<DraftResponse>)cached!;
                }
                activity?.SetTag("tool.cache.hit", false);
                
                var handler = serviceProvider.GetRequiredService<IHandler<ListDraftsByLocationQuery, List<DraftResponse>>>();
          
                var drafts = await handler.HandleAsync(new ListDraftsByLocationQuery(companyId, location), ct);
                cache.Set(cacheKey, drafts);

                activity?.SetTag("tool.result.count", drafts.Count);
                return drafts;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_list_by_location",
                Description =
                    "Returns the list of drafts for a company filtered by US location (e.g. Iowa City, IA; TX). Requires companyId and location.  State must be a 2-letter uppercase code (e.g. TX, CA, NY). City/State must be in the format 'City, State'. eg(Iowa City, IA)"
            });
        
        yield return AIFunctionFactory.Create(
            async (Guid companyId, string location, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity("tool.draft_count_by_location", ActivityKind.Internal);
                
                var normalizedLocation = location.Trim().ToUpperInvariant();
                activity?.AddTag("ai.operation", "draft_count_by_location");
                activity?.AddTag("tool.company_id", companyId);
                activity?.AddTag("tool.location", normalizedLocation);
                
                var cacheKey = $"draft_list_by_location:{companyId}:{normalizedLocation}";
                if (cache.TryGet(cacheKey, out var cached))
                {
                    activity?.SetTag("tool.cache.hit", true);
                    return ((List<DraftResponse>)cached!).Count;
                }
                activity?.SetTag("tool.cache.hit", false);
                var handler = serviceProvider.GetRequiredService<IHandler<ListDraftsByLocationQuery, List<DraftResponse>>>();
        
                var drafts = await handler.HandleAsync(new ListDraftsByLocationQuery(companyId, location), ct);
                cache.Set(cacheKey, drafts);

                activity?.SetTag("tool.result.count", drafts.Count);
                return drafts.Count;
            },
            new AIFunctionFactoryOptions
            {
                Name = "draft_count_by_location",
                Description =
                    "Returns the number of drafts for a company filtered by US location (e.g. Iowa City, IA; TX). Requires companyId and location.  State must be a 2-letter uppercase code (e.g. TX, CA, NY).  City/State must be in the format 'City, State'. eg(Iowa City, IA)"
            });
        
        yield return AIFunctionFactory.Create(
            (string input) =>
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["texas"] = "TX",
                    ["tx"] = "TX",
                    ["california"] = "CA",
                    ["ca"] = "CA"
                };
    
                return map.TryGetValue(input.Trim(), out var state)
                    ? state
                    : null;
            },
            new AIFunctionFactoryOptions
            {
                Name = "normalize_state",
                Description =
                    "Converts a US state name or abbreviation into a 2-letter uppercase state code. Returns null if unknown."
            });
    }
}