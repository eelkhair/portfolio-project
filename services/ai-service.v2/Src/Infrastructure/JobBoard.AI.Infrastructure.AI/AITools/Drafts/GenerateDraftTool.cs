using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class GenerateDraftTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver)
    {
        return AIFunctionFactory.Create(
            async (Guid companyId,  GenerateDraftCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.generate_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "generate_draft");
                activity?.AddTag("tool.company_id", cmd.Request.Brief);
                activity?.AddTag("tool.company_id", cmd.Request.TitleSeed);
                
                activity?.AddTag("tool.company_id", cmd.CompanyId);

                var handler = toolResolver.Resolve<GenerateDraftCommand, DraftResponse>();

                return await handler.HandleAsync(cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "generate_draft",
                Description = "Use AI to generate a draft for a company. companyId is required. Ensure CompanyId is populated in the request"
            });
    }
}