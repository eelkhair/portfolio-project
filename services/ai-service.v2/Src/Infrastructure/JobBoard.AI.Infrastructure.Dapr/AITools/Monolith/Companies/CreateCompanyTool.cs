using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;

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

public class CreateCompanyCommand
{
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public Guid IndustryUId { get; set; }
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}