using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;

public static class UpdateCompanyTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client)
    {
        return AIFunctionFactory.Create(
            async (UpdateCompanyCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.update_company",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "update_company");
                activity?.AddTag("tool.company_id", cmd.CompanyId);

                return await client.UpdateCompanyAsync(cmd.CompanyId, cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "update_company",
                Description = "Updates an existing company. This is a full replacement — you MUST provide ALL fields, not just the ones being changed. " +
                              "ALWAYS call company_list first to get the current values, then include every field (companyId, name, companyEmail, industryUId are required). " +
                              "Omitted fields will be set to null/empty."
            });
    }
}
