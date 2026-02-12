using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Tools;

public class AiServiceTools : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
    
        yield return AIFunctionFactory.Create(
            async (SaveDraftCommand cmd, IServiceProvider sp, CancellationToken ct) =>
            {
                var handler = sp.GetRequiredService<SaveDraftCommandHandler>();
                return await handler.HandleAsync(cmd, ct);
            },
            new AIFunctionFactoryOptions
            {
                Name = "save_draft",
                Description = "Saves a draft for a company."
            });

    } 
}