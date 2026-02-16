using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class SaveDraftTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver)
    {
        return AIFunctionFactory.Create(
            async (SaveDraftCommand cmd, CancellationToken ct) =>
                await ToolHelper.ExecuteAsync(activityFactory, "save_draft",
                    async (_, token) =>
                    {
                        var handler = toolResolver.Resolve<SaveDraftCommand, SaveDraftResponse>();
                        return await handler.HandleAsync(cmd, token);
                    }, ct,
                    ("tool.company_id", cmd.CompanyId)),
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company. companyId is required. Ensure CompanyId is populated"
            });
    }
}
