using System.ComponentModel;
using System.Text.Json;
using JobBoard.API.Mcp.Infrastructure;
using JobBoard.Application.Actions.Drafts.Delete;
using JobBoard.Application.Actions.Drafts.Get;
using JobBoard.Application.Actions.Drafts.List;
using JobBoard.Application.Actions.Drafts.ListByCompany;
using JobBoard.Application.Actions.Drafts.Save;
using JobBoard.Monolith.Contracts.Drafts;
using ModelContextProtocol.Server;

namespace JobBoard.API.Mcp.Tools;

[McpServerToolType]
public class DraftTools(HandlerDispatcher dispatcher)
{
    [McpServerTool(Name = "draft_list"),
     Description("Returns drafts for a company. Optionally filter by location (2-letter state code e.g. CA, NY).")]
    public async Task<string> ListDrafts(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] Guid companyId,
        [Description("Optional location filter (2-letter state code e.g. CA, NY)")] string? location = null,
        CancellationToken ct = default)
    {
        var query = new ListDraftsQuery { CompanyId = companyId };
        var drafts = await dispatcher.DispatchAsync<ListDraftsQuery, List<DraftResponse>>(query, ct);

        if (!string.IsNullOrWhiteSpace(location))
        {
            drafts = FilterByLocation(
                    drafts.Select(d => (d.Location, (object)d)).ToList(), location)
                .Cast<DraftResponse>().ToList();
        }

        var slim = drafts.Select(d => new
        {
            d.Id,
            d.Title,
            d.Location,
            d.JobType,
            d.SalaryRange
        });
        return JsonSerializer.Serialize(slim, Json.Opts);
    }

    [McpServerTool(Name = "draft_detail"),
     Description("Returns full details for a single draft by ID including aboutRole, responsibilities, qualifications, and notes.")]
    public async Task<string> GetDraft(
        [Description("The draft's unique identifier")] Guid draftId,
        CancellationToken ct)
    {
        try
        {
            var query = new GetDraftByIdQuery { DraftId = draftId };
            var draft = await dispatcher.DispatchAsync<GetDraftByIdQuery, DraftResponse>(query, ct);
            return JsonSerializer.Serialize(draft, Json.Opts);
        }
        catch
        {
            return JsonSerializer.Serialize(new { error = $"Draft '{draftId}' not found." }, Json.Opts);
        }
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
        var command = new SaveDraftCommand
        {
            CompanyId = companyId,
            Draft = new DraftResponse
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
            }
        };

        var result = await dispatcher.DispatchAsync<SaveDraftCommand, DraftResponse>(command, ct);
        return JsonSerializer.Serialize(new { result.Id, result.Title, status = "saved" }, Json.Opts);
    }

    [McpServerTool(Name = "delete_draft"), Description("Deletes a draft. Requires both companyId and draftId.")]
    public async Task<string> DeleteDraft(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] Guid companyId,
        [Description("The draft's unique identifier")] Guid draftId,
        CancellationToken ct)
    {
        var command = new DeleteDraftCommand { CompanyId = companyId, DraftId = draftId };
        await dispatcher.DispatchAsync<DeleteDraftCommand, bool>(command, ct);
        return JsonSerializer.Serialize(new { success = true, draftId }, Json.Opts);
    }

    [McpServerTool(Name = "drafts_by_company"),
     Description("Returns draft counts and titles grouped by company.")]
    public async Task<string> DraftsByCompany(CancellationToken ct)
    {
        var query = new ListAllDraftsByCompanyQuery();
        var result = await dispatcher.DispatchAsync<ListAllDraftsByCompanyQuery, Dictionary<Guid, DraftsByCompanyItemResponse>>(query, ct);
        var slim = result.ToDictionary(
            kv => kv.Key,
            kv => new
            {
                kv.Value.Count,
                Drafts = kv.Value.Drafts.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Location,
                    d.JobType
                })
            });
        return JsonSerializer.Serialize(slim, Json.Opts);
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
