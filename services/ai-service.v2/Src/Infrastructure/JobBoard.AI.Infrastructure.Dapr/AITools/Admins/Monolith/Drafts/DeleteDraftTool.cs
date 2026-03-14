using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Drafts;

public static class DeleteDraftTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, Guid draftId, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.delete_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "delete_draft");
                activity?.AddTag("tool.company_id", companyId);
                activity?.AddTag("tool.draft_id", draftId);

                await client.DeleteDraftAsync(companyId, draftId, ct);

                return new { success = true, draftId };
            },
            new AIFunctionFactoryOptions
            {
                Name = "delete_draft",
                Description = "Deletes a draft. Requires both companyId and draftId."
            });
    }
}
