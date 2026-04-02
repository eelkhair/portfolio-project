using System.ComponentModel;
using System.Text.Json;
using JobBoard.API.Mcp.Infrastructure;
using JobBoard.Application.Actions.Companies.Get;
using JobBoard.Application.Actions.Drafts.Get;
using JobBoard.Application.Actions.Jobs.Create;
using JobBoard.Application.Actions.Jobs.List;
using JobBoard.Application.Actions.Public;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace JobBoard.API.Mcp.Tools;

[McpServerToolType]
public class JobTools(HandlerDispatcher dispatcher, ILogger<JobTools> logger)
{
    [McpServerTool(Name = "job_list"),
     Description(
         "Returns detailed published jobs for a single company including responsibilities, qualifications, and about role. " +
         "Use only when you need these extra details beyond what company_job_summaries already provides.")]
    public async Task<string> ListJobs(
        [Description("The company's unique identifier")] Guid companyId,
        CancellationToken ct)
    {
        var query = new ListJobsQuery(companyId);
        var result = await dispatcher.DispatchAsync<ListJobsQuery, IQueryable<JobResponse>>(query, ct);
        var jobs = await result.ToListAsync(ct);
        return JsonSerializer.Serialize(jobs);
    }

    [McpServerTool(Name = "job_detail"),
     Description("Returns full details for a single job by ID including aboutRole, responsibilities, and qualifications.")]
    public async Task<string> GetJob(
        [Description("The job's unique identifier")] Guid jobId,
        CancellationToken ct)
    {
        try
        {
            var query = new GetPublicJobByIdQuery(jobId);
            var job = await dispatcher.DispatchAsync<GetPublicJobByIdQuery, JobResponse>(query, ct);
            return JsonSerializer.Serialize(job);
        }
        catch
        {
            return JsonSerializer.Serialize(new { error = $"Job '{jobId}' not found." });
        }
    }

    [McpServerTool(Name = "company_job_summaries"),
     Description(
         "Returns all companies with their published jobs (title, location, type, salary range, date) and job count in a single call. " +
         "ALWAYS use this tool first — it provides both summary counts AND full job listings for every company. " +
         "Only use job_list if you need additional job details like responsibilities or qualifications.")]
    public async Task<string> ListCompanyJobSummaries(CancellationToken ct)
    {
        var query = new GetCompanyJobSummariesQuery();
        var result = await dispatcher.DispatchAsync<GetCompanyJobSummariesQuery, IQueryable<CompanyJobSummaryDto>>(query, ct);
        var summaries = await result.ToListAsync(ct);
        return JsonSerializer.Serialize(summaries);
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

        // Fetch the draft first
        var draftQuery = new GetDraftByIdQuery { DraftId = id };
        DraftResponse draft;
        try
        {
            draft = await dispatcher.DispatchAsync<GetDraftByIdQuery, DraftResponse>(draftQuery, ct);
        }
        catch
        {
            return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." });
        }

        var request = new CreateJobRequest
        {
            Title = draft.Title,
            Location = draft.Location,
            JobType = Enum.TryParse<JobType>(draft.JobType, true, out var jt)
                ? jt
                : JobType.FullTime,
            AboutRole = draft.AboutRole,
            SalaryRange = draft.SalaryRange,
            Responsibilities = draft.Responsibilities,
            Qualifications = draft.Qualifications,
            CompanyUId = companyId,
            DraftId = draftId,
            DeleteDraft = deleteDraft
        };

        var command = new CreateJobCommand(request);
        var result = await dispatcher.DispatchAsync<CreateJobCommand, JobResponse>(command, ct);

        logger.LogInformation("Job created from draft {DraftId}", draftId);

        return JsonSerializer.Serialize(new { result.Id, result.Title, result.CompanyName, status = "published" });
    }
}
