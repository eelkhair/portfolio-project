using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Shared;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admins.Monolith.Jobs;

public static class CreateJobTool
{
    public static AIFunction Get(IActivityFactory activityFactory, IMonolithApiClient client, ILoggerFactory loggerFactory, IAiNotificationHub notificationHub, IUserAccessor userAccessor)
    {
        var logger = loggerFactory.CreateLogger(typeof(CreateJobTool));
        return AIFunctionFactory.Create(
            async Task<object> (Guid companyId, string draftId, bool deleteDraft, CancellationToken ct) =>
            {
                using var activity = activityFactory.StartActivity(
                    "tool.create_job",
                    ActivityKind.Internal);

                activity?.AddTag("ai.operation", "create_job");
                activity?.AddTag("draftId", draftId);

                logger.LogInformation("Creating job from draft {DraftId}, deleteDraft={DeleteDraft}", draftId, deleteDraft);

                if (!Guid.TryParse(draftId, out var id))
                {
                    logger.LogWarning("Invalid draftId format: {DraftId}", draftId);
                    return new { error = "Invalid draftId format. Must be a valid GUID." };
                }

                var content = await client.GetDraftByIdAsync(id, ct);

                if (content is null)
                {
                    logger.LogWarning("Draft {DraftId} not found", draftId);
                    return new { error = $"Draft '{draftId}' not found." };
                }

                logger.LogInformation("Draft {DraftId} resolved — Title: {Title}",
                    draftId, content.Title);

                var request = new CreateJobRequest
                {
                    Title = content.Title,
                    Location = content.Location,
                    JobType = content.JobType,
                    AboutRole = content.AboutRole,
                    SalaryRange = content.SalaryRange,
                    Responsibilities = content.Responsibilities,
                    Qualifications = content.Qualifications,
                    CompanyUId = companyId,
                    DraftId = draftId,
                    DeleteDraft = deleteDraft
                };

                var result = await client.CreateJobAsync(request, ct);

                logger.LogInformation("Job created from draft {DraftId} via monolith-api", draftId);

                var userId = userAccessor.UserId
                             ?? throw new InvalidOperationException("UserId is required for AI notifications.");

                var jobId = draftId;
                if (result.Data is JsonElement je && je.TryGetProperty("id", out var idProp))
                    jobId = idProp.GetString() ?? draftId;

                await notificationHub.SendToUserAsync(
                    userId,
                    AiNotificationMethods.Notification,
                    new AiNotificationDto(
                        Type: "job.published",
                        Title: content.Title,
                        EntityId: jobId,
                        EntityType: "job",
                        TraceParent: Activity.Current?.Id,
                        TraceState: Activity.Current?.TraceStateString,
                        CorrelationId: activity?.TraceId.ToString(),
                        Timestamp: DateTimeOffset.UtcNow,
                        Metadata: new Dictionary<string, object>()
                    ), ct);

                return result;
            },
            new AIFunctionFactoryOptions
            {
                Name = "create_job",
                Description =
                    "Creates a job from an existing draft. Requires companyId, draftId and deleteDraft (bool). Before calling this tool, you MUST ask the user whether they want to delete the draft after publishing and use their answer for the deleteDraft parameter."
            });
    }
}
