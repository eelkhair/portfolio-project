using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Shared;

public static class CreateCompanyTool
{
    public static AIFunction Get<TRequest>(
        IActivityFactory activityFactory,
        Func<TRequest, CancellationToken, Task<object>> createCompany)
    {
        return AIFunctionFactory.Create(
            async (TRequest cmd, CancellationToken ct) =>
                await ToolHelper.ExecuteAsync(activityFactory, "create_company",
                    async (_, token) => await createCompany(cmd, token), ct),
            new AIFunctionFactoryOptions
            {
                Name = "create_company",
                Description = "Creates a company. "
            });
    }
}
