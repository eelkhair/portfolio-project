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
    [McpServerTool(Name = "draft_list"), Description("Returns a list of drafts for a company.")]
    public async Task<string> ListDrafts(
        [Description("The company's unique identifier")] Guid companyId,
        CancellationToken ct)
    {
        var response = await queryService.ListDrafts(companyId.ToString(), ct);
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "save_draft"),
     Description("Saves a draft for a company. companyId is required. Ensure CompanyId is populated.")]
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
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "draft_list_by_location"),
     Description(
         "Returns the list of drafts by location for a company. " +
         "State must be normalized to 2 letter code (eg. CA, NY, IA, TX). " +
         "Remove any drafts that are not in the specified location after the tool is done processing.")]
    public async Task<string> ListDraftsByLocation(
        [Description("The company's unique identifier")] Guid companyId,
        [Description("State or city filter (use 2-letter state code e.g. CA, NY)")] string location,
        CancellationToken ct)
    {
        var response = await queryService.ListDrafts(companyId.ToString(), ct);
        var drafts = response.Data ?? [];
        var filtered = FilterByLocation(
            drafts.Select(d => (d.Location, (object)d)).ToList(), location)
            .Cast<JobDraftResponse>().ToList();
        return JsonSerializer.Serialize(filtered);
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
