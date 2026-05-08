using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Demo;

public static class ListIndustriesTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IMonolithApiClient monolithClient,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("Optional substring filter applied case-insensitively to industry names. Pass empty string for the full catalog.")]
                string filter,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.list_industries",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "list_industries");
                activity?.SetTag("industries.filter", filter);

                logger.LogInformation("Listing industries (filter: {Filter})", filter);

                var industries = await monolithClient.ListDemoIndustriesAsync(ct);

                var filtered = string.IsNullOrWhiteSpace(filter)
                    ? industries
                    : industries
                        .Where(i => i.Name?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();

                activity?.SetTag("industries.total", industries.Count);
                activity?.SetTag("industries.matched", filtered.Count);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    count = filtered.Count,
                    industries = filtered.Select(i => new { id = i.Id, name = i.Name })
                });
            },
            new AIFunctionFactoryOptions
            {
                Name = "list_industries",
                Description =
                    "Returns the platform's industry catalog (id + name). Use this when the visitor asks " +
                    "to see industry options before naming their demo company, or when you want to surface " +
                    "a few examples to help them pick. Optional case-insensitive substring filter."
            });
    }
}
