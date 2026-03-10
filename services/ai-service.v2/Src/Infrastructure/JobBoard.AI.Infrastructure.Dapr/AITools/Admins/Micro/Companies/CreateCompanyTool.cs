using System.Diagnostics;
using AdminAPI.Contracts.Models.Companies.Requests;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Micro.Companies;

public static class CreateCompanyTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client)
    {
        return AIFunctionFactory.Create(
            async (CreateCompanyRequest cmd, CancellationToken ct) =>
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
