using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Companies;

public static class CreateCompanyTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client)
    {
        return AIFunctionFactory.Create(
            async (CreateCompanyCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.create_company",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "create_company");

                return await client.CreateCompanyAsync(cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "create_company",
                Description = "Creates a company. "
            });

    }
}
