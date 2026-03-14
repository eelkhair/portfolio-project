using System.ComponentModel;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Infrastructure.AI.AITools.Admins.Models;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.MCP.Micro.Tools.Admin;

[McpServerToolType]
public class JobTools(
    IAdminApiClient adminClient,
    IMonolithApiClient monolithClient,
    IConfiguration configuration,
    IUserAccessor userAccessor,
    IAiNotificationHub notificationHub,
    ILogger<JobTools> logger)
{
    private bool IsMonolith => configuration.GetValue<bool>("FeatureFlags:Monolith");

    [McpServerTool(Name = "job_list"),
     Description(
         "Returns detailed published jobs for a single company including responsibilities, qualifications, and about role. " +
         "Use only when you need these extra details beyond what company_job_summaries already provides.")]
    public async Task<string> ListJobs(
        [Description("The company's unique identifier")] Guid companyId,
        CancellationToken ct)
    {
        if (IsMonolith)
        {
            var result = await monolithClient.ListJobsAsync(companyId, ct);
            return JsonSerializer.Serialize(result);
        }

        var response = await adminClient.ListJobsAsync(companyId, ct);
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "company_job_summaries"),
     Description(
         "Returns all companies with their published jobs (title, location, type, salary range, date) and job count in a single call. " +
         "ALWAYS use this tool first — it provides both summary counts AND full job listings for every company. " +
         "Only use job_list if you need additional job details like responsibilities or qualifications.")]
    public async Task<string> ListCompanyJobSummaries(CancellationToken ct)
    {
        if (IsMonolith)
        {
            var result = await monolithClient.ListCompanyJobSummariesAsync(ct);
            return JsonSerializer.Serialize(result);
        }

        var response = await adminClient.ListCompanyJobSummariesAsync(ct);
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "create_job"),
     Description(
         "Creates a job from an existing draft. Requires companyId, draftId and deleteDraft (bool). " +
         "Before calling this tool, you MUST ask the user whether they want to delete the draft after publishing " +
         "and use their answer for the deleteDraft parameter.")]
    public async Task<string> CreateJob(
        [Description("The company's unique identifier")] Guid companyId,
        [Description("The draft GUID to publish as a job")] string draftId,
        [Description("Whether to delete the draft after publishing")] bool deleteDraft,
        CancellationToken ct)
    {
        if (!Guid.TryParse(draftId, out var id))
        {
            logger.LogWarning("Invalid draftId format: {DraftId}", draftId);
            return JsonSerializer.Serialize(new { error = "Invalid draftId format. Must be a valid GUID." });
        }

        string title;
        object result;

        if (IsMonolith)
        {
            var content = await monolithClient.GetDraftByIdAsync(id, ct);
            if (content is null)
                return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." });

            title = content.Title;
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
            result = await monolithClient.CreateJobAsync(request, ct);
        }
        else
        {
            var draftResponse = await adminClient.GetDraftByIdAsync(id, ct);
            if (!draftResponse.Success || draftResponse.Data is null)
                return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." });

            var content = draftResponse.Data;
            title = content.Title;
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
            result = await adminClient.CreateJobAsync(request, ct);
        }

        logger.LogInformation("Job created from draft {DraftId}", draftId);

        var userId = userAccessor.UserId
                     ?? throw new InvalidOperationException("UserId is required for AI notifications.");

        await notificationHub.SendToUserAsync(
            userId,
            AiNotificationMethods.Notification,
            new AiNotificationDto(
                Type: "job.published",
                Title: title,
                EntityId: draftId,
                EntityType: "job",
                TraceParent: null,
                TraceState: null,
                CorrelationId: null,
                Timestamp: DateTimeOffset.UtcNow,
                Metadata: new Dictionary<string, object>()
            ), ct);

        return JsonSerializer.Serialize(result);
    }
}
