using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Services;
using JobAPI.Contracts.Enums;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace AdminApi.Mcp.Tools;

[McpServerToolType]
public class JobTools(
    IJobCommandService commandService,
    IJobQueryService queryService,
    ILogger<JobTools> logger)
{
    [McpServerTool(Name = "job_list"),
     Description(
         "Returns detailed published jobs for a single company including responsibilities, qualifications, and about role. " +
         "Use only when you need these extra details beyond what company_job_summaries already provides.")]
    public async Task<string> ListJobs(
        [Description("The company's unique identifier")] Guid companyId,
        CancellationToken ct)
    {
        var response = await queryService.ListAsync(companyId, ct);
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "company_job_summaries"),
     Description(
         "Returns all companies with their published jobs (title, location, type, salary range, date) and job count in a single call. " +
         "ALWAYS use this tool first — it provides both summary counts AND full job listings for every company. " +
         "Only use job_list if you need additional job details like responsibilities or qualifications.")]
    public async Task<string> ListCompanyJobSummaries(CancellationToken ct)
    {
        var response = await queryService.ListCompanyJobSummariesAsync(ct);
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

        var draftResponse = await queryService.GetDraft(id, ct);
        if (!draftResponse.Success || draftResponse.Data is null)
            return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." });

        var content = draftResponse.Data;
        var request = new JobCreateRequest
        {
            Title = content.Title,
            Location = content.Location,
            JobType = Enum.TryParse<JobType>(content.JobType, true, out var jt) ? jt : JobType.FullTime,
            AboutRole = content.AboutRole,
            SalaryRange = content.SalaryRange,
            Responsibilities = content.Responsibilities,
            Qualifications = content.Qualifications,
            CompanyUId = companyId,
            DraftId = draftId,
            DeleteDraft = deleteDraft
        };

        var result = await commandService.CreateJob(request, ct);

        logger.LogInformation("Job created from draft {DraftId}", draftId);

        return JsonSerializer.Serialize(result);
    }
}
