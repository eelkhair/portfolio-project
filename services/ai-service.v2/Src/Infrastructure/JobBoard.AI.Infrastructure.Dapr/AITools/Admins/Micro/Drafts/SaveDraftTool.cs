using System.Diagnostics;
using AdminAPI.Contracts.Models.Jobs.Responses;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Micro.Drafts;

public static class SaveDraftTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId, JobDraftResponse draft, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.save_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "save_draft");
                activity?.AddTag("tool.company_id", companyId);

                var response = await client.SaveDraftAsync(companyId, draft, ct);

                activity?.SetTag("tool.draft.id", response.Data?.Id);

                return response.Data;
            },
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company. companyId is required. Ensure CompanyId is populated."
            });
    }
}
