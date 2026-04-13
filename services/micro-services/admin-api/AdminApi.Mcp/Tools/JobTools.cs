using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Services;
using JobAPI.Contracts.Enums;
using ModelContextProtocol.Server;

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
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        CancellationToken ct)
    {
        var (ok, id, err) = Json.ParseGuid(companyId, "companyId");
        if (!ok) return err!;

        var response = await queryService.ListAsync(id, ct);
        return JsonSerializer.Serialize(response.Data, Json.Opts);
    }

    [McpServerTool(Name = "job_detail"),
     Description("Returns full details for a single job by ID including aboutRole, responsibilities, and qualifications.")]
    public async Task<string> GetJob(
        [Description("The job's unique identifier (GUID)")] string jobId,
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        CancellationToken ct)
    {
        var (ok1, cId, err1) = Json.ParseGuid(companyId, "companyId");
        if (!ok1) return err1!;
        var (ok2, jId, err2) = Json.ParseGuid(jobId, "jobId");
        if (!ok2) return err2!;

        var response = await queryService.ListAsync(cId, ct);
        var job = response.Data?.FirstOrDefault(j => j.UId == jId);

        if (job is null)
            return JsonSerializer.Serialize(new { error = $"Job '{jobId}' not found." }, Json.Opts);

        return JsonSerializer.Serialize(job, Json.Opts);
    }

    [McpServerTool(Name = "company_job_summaries"),
     Description(
         "Returns all companies with their published jobs (title, location, type, salary range, date) and job count in a single call. " +
         "ALWAYS use this tool first — it provides both summary counts AND full job listings for every company. " +
         "Only use job_list if you need additional job details like responsibilities or qualifications.")]
    public async Task<string> ListCompanyJobSummaries(CancellationToken ct)
    {
        var response = await queryService.ListCompanyJobSummariesAsync(ct);
        return JsonSerializer.Serialize(response.Data, Json.Opts);
    }

    [McpServerTool(Name = "create_job"),
     Description(
         "Creates a job from an existing draft. Requires companyId, draftId and deleteDraft (bool). " +
         "Before calling this tool, you MUST ask the user whether they want to delete the draft after publishing " +
         "and use their answer for the deleteDraft parameter.")]
    public async Task<string> CreateJob(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        [Description("The draft GUID to publish as a job")] string draftId,
        [Description("Whether to delete the draft after publishing")] bool deleteDraft,
        CancellationToken ct)
    {
        var (ok1, cId, err1) = Json.ParseGuid(companyId, "companyId");
        if (!ok1) return err1!;
        var (ok2, dId, err2) = Json.ParseGuid(draftId, "draftId");
        if (!ok2) return err2!;

        var draftResponse = await queryService.GetDraft(dId, ct);
        if (!draftResponse.Success || draftResponse.Data is null)
            return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." }, Json.Opts);

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
            CompanyUId = cId,
            DraftId = draftId,
            DeleteDraft = deleteDraft
        };

        var result = await commandService.CreateJob(request, ct);

        logger.LogInformation("Job created from draft {DraftId}", draftId);

        return JsonSerializer.Serialize(new { Id = result.Data?.UId, result.Data?.Title, result.Data?.CompanyName, status = "published" }, Json.Opts);
    }
}
