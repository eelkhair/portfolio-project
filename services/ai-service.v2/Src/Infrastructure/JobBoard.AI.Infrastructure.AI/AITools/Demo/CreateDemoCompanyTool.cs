using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Demo;

public static class CreateDemoCompanyTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IMonolithApiClient monolithClient,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("The name of the company to create. Required. Use exactly what the visitor told you.")]
                string companyName,
                [Description(
                    "Free-text industry hint from the visitor (e.g., 'tech', 'finance', 'retail'). " +
                    "Optional. The server fuzzy-matches against the industry catalog. Pass empty string if the visitor didn't specify.")]
                string industryHint,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.create_demo_company",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "create_demo_company");
                activity?.SetTag("company.name", companyName);
                activity?.SetTag("company.industry_hint", industryHint);
                activity?.SetTag("company.is_demo", true);

                logger.LogInformation(
                    "Creating demo company {CompanyName} (industry hint: {IndustryHint})",
                    companyName, industryHint);

                try
                {
                    var result = await monolithClient.CreateDemoCompanyAsync(
                        new CreateDemoCompanyRequest
                        {
                            Name = companyName,
                            IndustryHint = string.IsNullOrWhiteSpace(industryHint) ? null : industryHint,
                        },
                        ct);

                    activity?.SetTag("company.uid", result.Company.Id.ToString());
                    activity?.SetTag("company.demo_expires_at", result.DemoExpiresAt.ToString("O"));

                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        companyUId = result.Company.Id,
                        companyName = result.Company.Name,
                        demoExpiresAt = result.DemoExpiresAt,
                        claimToken = result.ClaimToken,
                        traceId = result.TraceId,
                        message =
                            $"Demo company '{result.Company.Name}' created. " +
                            $"It will auto-delete at {result.DemoExpiresAt:HH:mm} UTC. " +
                            "The widget will surface Jaeger + Grafana links and a Claim button."
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Demo company creation failed for {CompanyName}", companyName);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = "Demo company creation failed. The visitor's request did not produce a real company."
                    });
                }
            },
            new AIFunctionFactoryOptions
            {
                Name = "create_demo_company",
                Description =
                    "Creates a real demo company in the JobBoard system, end-to-end through the same flow real " +
                    "admins use (monolith handler → RabbitMQ event → connector-api saga → microservices → " +
                    "Keycloak). The company auto-deletes after 1 hour. " +
                    "Call this tool ONCE per conversation, only after collecting at least the company name. " +
                    "Do NOT ask the visitor for an admin email — the server synthesizes one. The tool returns " +
                    "the new company's UID, the auto-delete timestamp, and a claim token the widget will use."
            });
    }
}
