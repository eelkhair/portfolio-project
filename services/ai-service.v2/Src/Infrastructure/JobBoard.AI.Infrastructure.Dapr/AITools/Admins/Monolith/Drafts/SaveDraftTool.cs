using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Drafts;

public static class SaveDraftTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, DraftResponse draft, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.save_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "save_draft");
                activity?.AddTag("tool.company_id", companyId);

                var result = await client.SaveDraftAsync(companyId, draft, ct);

                activity?.SetTag("tool.draft.id", result.Id);

                return result;
            },
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company. companyId is required. Ensure CompanyId is populated."
            });
    }
}
