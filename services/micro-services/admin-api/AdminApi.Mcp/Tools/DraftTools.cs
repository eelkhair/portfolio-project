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
        [Description("The company's unique identifier")] Guid companyId,
        [Description("Optional location filter (2-letter state code e.g. CA, NY)")] string? location = null,
        CancellationToken ct = default)
    {
        var response = await queryService.ListDrafts(companyId.ToString(), ct);
        var drafts = response.Data ?? [];

        if (!string.IsNullOrWhiteSpace(location))
        {
            drafts = FilterByLocation(
                    drafts.Select(d => (d.Location, (object)d)).ToList(), location)
                .Cast<JobDraftResponse>().ToList();
        }

        var slim = drafts.Select(d => new { d.Id, d.Title, d.Location, d.JobType, d.SalaryRange });
        return JsonSerializer.Serialize(slim);
    }

    [McpServerTool(Name = "draft_detail"),
     Description("Returns full details for a single draft by ID including aboutRole, responsibilities, qualifications, and notes.")]
    public async Task<string> GetDraft(
        [Description("The draft's unique identifier")] Guid draftId,
        CancellationToken ct)
    {
        var response = await queryService.GetDraft(draftId, ct);
        if (!response.Success || response.Data is null)
            return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." });

        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "save_draft"),
     Description("Saves a draft for a company. companyId is required.")]
    public async Task<string> SaveDraft(
        [Description("The company's unique identifier (required)")] Guid companyId,
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

        var response = await commandService.CreateDraft(companyId.ToString(), request, ct);
        var data = response.Data;
        return JsonSerializer.Serialize(new { data?.Id, data?.Title, status = "saved" });
    }

    [McpServerTool(Name = "delete_draft"), Description("Deletes a draft. Requires both companyId and draftId.")]
    public async Task<string> DeleteDraft(
        [Description("The company's unique identifier")] Guid companyId,
        [Description("The draft's unique identifier")] Guid draftId,
        CancellationToken ct)
    {
        await commandService.DeleteDraft(companyId.ToString(), draftId, ct);
        return JsonSerializer.Serialize(new { success = true, draftId });
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
