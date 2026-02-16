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
                activity?.AddTag("tool.Title", cmd.Request.TitleSeed?[..128]);
                activity?.AddTag("tool.Brief", cmd.Request.Brief?[..512]);
                
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
                        Title: $"Draft generated for {cmd.Request.TitleSeed}",
                        EntityId: result.Id,
                        EntityType: "draft",
                        CorrelationId: activity?.TraceId.ToString(),
                        Timestamp: DateTimeOffset.UtcNow
                    ),
                    ct
                );
                return new
                {
                    Status = "DraftGenerated",
                    DraftId = result.Id
                };
            },
            new AIFunctionFactoryOptions
            {
                Name = "generate_draft",
                Description = "Use AI to generate a draft for a company. companyId is required. Ensure CompanyId is populated in the request"
            });
    }
}