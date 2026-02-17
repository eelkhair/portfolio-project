using System.Diagnostics;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;

namespace JobBoard.AI.Infrastructure.AI.AITools.Drafts;

public static class GenerateDraftTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IAiToolHandlerResolver toolResolver, IAiNotificationHub notificationHub, IUserAccessor userAccessor)
    {
        return AIFunctionFactory.Create(
            async (GenerateDraftCommand cmd, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.generate_draft",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "generate_draft");
                activity?.AddTag("tool.Title", cmd.Request.TitleSeed);
                
                activity?.AddTag("tool.company_id", cmd.CompanyId);

                var handler = toolResolver.Resolve<GenerateDraftCommand, DraftResponse>();

                var result = await handler.HandleAsync(cmd, ct);
                
                var userId = userAccessor.UserId 
                             ?? throw new InvalidOperationException("UserId is required for AI notifications.");
                
                await notificationHub.SendToUserAsync(
                    userId,
                    AiNotificationMethods.Notification,
                    new AiNotificationDto(
                        Type: "draft.generated",
                        Title: $"{cmd.Request.TitleSeed}",
                        EntityId: result.Id,
                        EntityType: "draft",
                        TraceParent: Activity.Current?.Id,                 
                        TraceState: Activity.Current?.TraceStateString, 
                        CorrelationId: activity?.TraceId.ToString(),
                        Timestamp: DateTimeOffset.UtcNow,
                        Metadata: new Dictionary<string, object>
                        {
                            { "companyId", cmd.CompanyId },
                            { "companyName", cmd.Request.CompanyName ?? string.Empty }
                        }
                    ),
                    ct
                );
                return result;
            },
            new AIFunctionFactoryOptions
            {
                Name = "generate_draft",
                Description =
                    "Generates a job draft for a company using AI and automatically saves it to the database. " +
                    "Required fields: companyId, companyName, team, titleSeed,about role(brief), job-type." +
                    "Only call this function when all required fields are available."
            });
    }
}