using System.ComponentModel;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.MCP.Integration.Tools.Admin;

[McpServerToolType]
public class SystemTools(
    IAiToolHandlerResolver toolResolver,
    IDraftPersistence draftPersistence,
    IAiNotificationHub notificationHub,
    IUserAccessor userAccessor,
    ISettingsService settingsService,
    ILogger<SystemTools> logger)
{
    [McpServerTool(Name = "generate_draft"),
     Description(
         "Generates a job draft for a company using AI and automatically saves it to the database. " +
         "Required fields: companyId, companyName, team, titleSeed, aboutRole (brief), jobType. " +
         "Only call this function when all required fields are available.")]
    public async Task<string> GenerateDraft(
        [Description("The company's unique identifier (required)")] Guid companyId,
        [Description("Brief description of the role to generate a draft for (required)")] string brief,
        [Description("Company name for context in the generated draft")] string? companyName = null,
        [Description("Team or department name")] string? teamName = null,
        [Description("Suggested job title")] string? titleSeed = null,
        [Description("Job location e.g. 'San Francisco, CA'")] string? location = null,
        [Description("Comma-separated tech stack e.g. 'C#, .NET, Azure'")] string? techStackCsv = null,
        [Description("Comma-separated must-have qualifications")] string? mustHavesCsv = null,
        [Description("Comma-separated nice-to-have qualifications")] string? niceToHavesCsv = null,
        [Description("Benefits and perks to include")] string? benefits = null,
        CancellationToken ct = default)
    {
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
                    { "companyName", companyName }
                }
            ), ct);

        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Name = "is_monolith"),
     Description(
         "Checks if the application is running in monolith or microservices mode based on feature flag configuration. " +
         "monolith = true, microservices = false. MUST be returned if the user asks about system configuration.")]
    public async Task<string> IsMonolith(CancellationToken ct)
    {
        var mode = await settingsService.GetApplicationModeAsync();
        return JsonSerializer.Serialize(new { isMonolith = mode.IsMonolith });
    }

    [McpServerTool(Name = "set_mode"),
     Description(
         "Sets the application mode to monolith or microservices based on the provided boolean flag. " +
         "true = monolith, false = microservices. " +
         "WARNING: This tool MUST ONLY be used when the user explicitly asks to change system mode.")]
    public async Task<string> SetMode(
        [Description("true for monolith mode, false for microservices mode")] bool isMonolith,
        CancellationToken ct)
    {
        logger.LogInformation("Setting application mode to {Mode}", isMonolith ? "monolith" : "microservices");
        await settingsService.UpdateApplicationModeAsync(new ApplicationModeDto { IsMonolith = isMonolith });
        return JsonSerializer.Serialize(new { success = true, isMonolith });
    }
}
