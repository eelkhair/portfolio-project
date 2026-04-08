using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using AdminAPI.Contracts.Services;
using ModelContextProtocol.Server;

namespace AdminApi.Mcp.Tools;

[McpServerToolType]
public class DraftTools(
    IJobCommandService commandService,
    IJobQueryService queryService)
{
    [McpServerTool(Name = "draft_list"),
     Description("Returns drafts for a company. Optionally filter by location (2-letter state code e.g. CA, NY).")]
    public async Task<string> ListDrafts(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        [Description("Optional location filter (2-letter state code e.g. CA, NY)")] string? location = null,
        CancellationToken ct = default)
    {
        var (ok, id, err) = Json.ParseGuid(companyId, "companyId");
        if (!ok) return err!;

        var response = await queryService.ListDrafts(id.ToString(), ct);
        var drafts = response.Data ?? [];

        if (!string.IsNullOrWhiteSpace(location))
        {
            drafts = FilterByLocation(
                    drafts.Select(d => (d.Location, (object)d)).ToList(), location)
                .Cast<JobDraftResponse>().ToList();
        }

        var slim = drafts.Select(d => new { d.Id, d.Title, d.Location, d.JobType, d.SalaryRange });
        return JsonSerializer.Serialize(slim, Json.Opts);
    }

    [McpServerTool(Name = "draft_detail"),
     Description("Returns full details for a single draft by ID including aboutRole, responsibilities, qualifications, and notes.")]
    public async Task<string> GetDraft(
        [Description("The draft's unique identifier (GUID)")] string draftId,
        CancellationToken ct)
    {
        var (ok, id, err) = Json.ParseGuid(draftId, "draftId");
        if (!ok) return err!;

        var response = await queryService.GetDraft(id, ct);
        if (!response.Success || response.Data is null)
            return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." }, Json.Opts);

        return JsonSerializer.Serialize(response.Data, Json.Opts);
    }

    [McpServerTool(Name = "save_draft"),
     Description("Saves a draft for a company. companyId is required.")]
    public async Task<string> SaveDraft(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        [Description("Job title (required)")] string title,
        [Description("Description of the role (required)")] string aboutRole,
        [Description("List of job responsibilities (required)")] List<string> responsibilities,
        [Description("List of required qualifications (required)")] List<string> qualifications,
        [Description("Job location e.g. 'San Francisco, CA' (required)")] string location,
        [Description("Job type e.g. 'Full-time', 'Part-time', 'Contract' (required)")] string jobType,
        [Description("Salary range e.g. '$120k - $150k' (required)")] string salaryRange,
        [Description("Additional notes (optional)")] string notes = "",
        [Description("Draft ID — provide to update an existing draft (optional)")] string? id = null,
        CancellationToken ct = default)
    {
        var (ok, cId, err) = Json.ParseGuid(companyId, "companyId");
        if (!ok) return err!;

        var request = new JobDraftRequest
        {
            Title = title,
            AboutRole = aboutRole,
            Responsibilities = responsibilities,
            Qualifications = qualifications,
            Location = location,
            JobType = jobType,
            SalaryRange = salaryRange,
            Notes = notes,
            Id = id
        };

        var response = await commandService.CreateDraft(cId.ToString(), request, ct);
        var data = response.Data;
        return JsonSerializer.Serialize(new { data?.Id, data?.Title, status = "saved" }, Json.Opts);
    }

    [McpServerTool(Name = "delete_draft"), Description("Deletes a draft. Requires both companyId and draftId.")]
    public async Task<string> DeleteDraft(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        [Description("The draft's unique identifier (GUID)")] string draftId,
        CancellationToken ct)
    {
        var (ok1, cId, err1) = Json.ParseGuid(companyId, "companyId");
        if (!ok1) return err1!;
        var (ok2, dId, err2) = Json.ParseGuid(draftId, "draftId");
        if (!ok2) return err2!;

        await commandService.DeleteDraft(cId.ToString(), dId, ct);
        return JsonSerializer.Serialize(new { success = true, draftId = dId }, Json.Opts);
    }

    private static List<object> FilterByLocation(List<(string? Location, object Item)> items, string location)
    {
        var parts = location
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        if (parts.Count > 1)
            parts[^1] = ", " + parts[^1];

        return items
            .Where(d =>
                !string.IsNullOrWhiteSpace(d.Location) &&
                parts.Any(p =>
                    d.Location!.EndsWith(p, StringComparison.OrdinalIgnoreCase) ||
                    d.Location!.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .Select(d => d.Item)
            .ToList();
    }
}
