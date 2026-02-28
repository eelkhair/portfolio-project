using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Infrastructure.Dapr.AITools.Shared;
using JobBoard.AI.Infrastructure.Dapr.ApiClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.AITools.Admin.Jobs;

public static class CreateJobTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static AIFunction Get(IActivityFactory activityFactory, IAdminApiClient client, IAiDbContext dbContext, ILoggerFactory loggerFactory, IAiNotificationHub notificationHub, IUserAccessor userAccessor)
    {
        var logger = loggerFactory.CreateLogger(typeof(CreateJobTool));
        return AIFunctionFactory.Create(
            async Task<object> (string draftId, bool deleteDraft, CancellationToken ct) =>
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

                var draft = await dbContext.Drafts
                    .FirstOrDefaultAsync(d => d.Id == id, ct);

                if (draft is null)
                {
                    logger.LogWarning("Draft {DraftId} not found", draftId);
                    return new { error = $"Draft '{draftId}' not found." };
                }

                var content = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOptions);
                if (content is null)
                {
                    logger.LogWarning("Failed to deserialize draft {DraftId} content", draftId);
                    return new { error = "Failed to deserialize draft content." };
                }

                logger.LogInformation("Draft {DraftId} resolved — Title: {Title}, Company: {CompanyId}",
                    draftId, content.Title, draft.CompanyId);

                var request = new CreateJobRequest
                {
                    Title = content.Title,
                    Location = content.Location,
                    JobType = content.JobType,
                    AboutRole = content.AboutRole,
                    SalaryRange = content.SalaryRange,
                    Responsibilities = content.Responsibilities,
                    Qualifications = content.Qualifications,
                    CompanyUId = draft.CompanyId,
                    DraftId = draftId,
                    DeleteDraft = deleteDraft
                };

                var result = await client.CreateJobAsync(request, ct);

                logger.LogInformation("Job created from draft {DraftId} via admin-api", draftId);

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
                        Metadata: new Dictionary<string, object>
                        {
                            { "companyId", draft.CompanyId }
                        }
                    ), ct);

                return result;
            },
            new AIFunctionFactoryOptions
            {
                Name = "create_job",
                Description =
                    "Creates a job from an existing draft. Takes the draftId and whether to delete the draft after publishing."
            });
    }
}
