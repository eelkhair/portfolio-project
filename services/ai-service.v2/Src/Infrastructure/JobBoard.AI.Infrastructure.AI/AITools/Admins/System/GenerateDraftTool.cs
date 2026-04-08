using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins.System;

public static class GenerateDraftTool
{
    public static AIFunction Get(
        IActivityFactory activityFactory,
        IAiToolHandlerResolver toolResolver,
        IDraftPersistence draftPersistence,
        IAiNotificationHub notificationHub,
        IUserAccessor userAccessor,
        ILogger logger)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("The company's unique identifier as a GUID (e.g. '019c7737-134b-74d1-8716-5af9de9793a3'). Get this from company_list UId field. Never pass a company name.")] Guid companyId,
                [Description("Brief description of the role to generate a draft for (required)")] string brief,
                [Description("Company name for context in the generated draft")] string? companyName,
                [Description("Team or department name")] string? teamName,
                [Description("Suggested job title")] string? titleSeed,
                [Description("Job location e.g. 'San Francisco, CA'")] string? location,
                [Description("Comma-separated tech stack e.g. 'C#, .NET, Azure'")] string? techStackCsv,
                [Description("Comma-separated must-have qualifications")] string? mustHavesCsv,
                [Description("Comma-separated nice-to-have qualifications")] string? niceToHavesCsv,
                [Description("Benefits and perks to include")] string? benefits,
                CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.generate_draft",
                    ActivityKind.Internal);

                activity?.SetTag("ai.operation", "generate_draft");
                activity?.SetTag("company.id", companyId.ToString());

                var request = new GenerateDraftRequest
                {
                    CompanyName = companyName,
                    TeamName = teamName,
                    TitleSeed = titleSeed,
                    Brief = brief,
                    Location = location,
                    TechStackCsv = techStackCsv,
                    MustHavesCsv = mustHavesCsv,
                    NiceToHavesCsv = niceToHavesCsv,
                    Benefits = benefits
                };

                var cmd = new GenerateDraftCommand(companyId, request);
                var handler = toolResolver.Resolve<GenerateDraftCommand, DraftResponse>();
                var result = await handler.HandleAsync(cmd, ct);

                var saved = await draftPersistence.SaveDraftAsync(companyId, result, ct);
                result.Id = saved.Id;

                logger.LogInformation("Draft generated for company {CompanyId}: {Title}", companyId, titleSeed ?? brief);

                var userId = userAccessor.UserId
                             ?? throw new InvalidOperationException("UserId is required for AI notifications.");

                await notificationHub.SendToUserAsync(
                    userId,
                    AiNotificationMethods.Notification,
                    new AiNotificationDto(
                        Type: "draft.generated",
                        Title: titleSeed ?? brief,
                        EntityId: result.Id,
                        EntityType: "draft",
                        TraceParent: null,
                        TraceState: null,
                        CorrelationId: null,
                        Timestamp: DateTimeOffset.UtcNow,
                        Metadata: new Dictionary<string, object>
                        {
                            { "companyId", companyId },
                            { "companyName", companyName ?? string.Empty }
                        }
                    ), ct);

                return JsonSerializer.Serialize(result);
            },
            new AIFunctionFactoryOptions
            {
                Name = "generate_draft",
                Description =
                    "Generates a job draft for a company using AI and automatically saves it to the database. " +
                    "DO NOT call this tool until you have collected all fields via the wizard flow described in the system prompt. " +
                    "You must have at minimum: companyId, brief, location, and techStackCsv before calling."
            });
    }
}
