using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Models.Jobs.Responses;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.Monolith.Contracts.Drafts;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Configuration;

namespace JobBoard.AI.MCP.Micro.Tools.Admin;

[McpServerToolType]
public class DraftTools(
    IAdminApiClient adminClient,
    IMonolithApiClient monolithClient,
    IConfiguration configuration)
{
    private bool IsMonolith => configuration.GetValue<bool>("FeatureFlags:Monolith");

    [McpServerTool(Name = "draft_list"), Description("Returns a list of drafts for a company.")]
    public async Task<string> ListDrafts(
        [Description("The company's unique identifier")] Guid companyId,
        CancellationToken ct)
    {
        if (IsMonolith)
        {
            var result = await monolithClient.ListDraftsAsync(companyId, ct);
            return JsonSerializer.Serialize(result);
        }

        var response = await adminClient.ListDraftsAsync(companyId, ct);
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
        if (IsMonolith)
        {
            var draft = new DraftResponse
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
            var result = await monolithClient.SaveDraftAsync(companyId, draft, ct);
            return JsonSerializer.Serialize(result);
        }

        var microDraft = new JobDraftResponse
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
        var response = await adminClient.SaveDraftAsync(companyId, microDraft, ct);
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
        if (IsMonolith)
        {
            var drafts = await monolithClient.ListDraftsAsync(companyId, ct);
            var filtered = FilterByLocation(drafts.Select(d => (d.Location, (object)d)).ToList(), location)
                .Cast<DraftResponse>().ToList();
            return JsonSerializer.Serialize(filtered);
        }

        var response = await adminClient.ListDraftsAsync(companyId, ct);
        var microDrafts = response.Data ?? [];
        var microFiltered = FilterByLocation(microDrafts.Select(d => (d.Location, (object)d)).ToList(), location)
            .Cast<JobDraftResponse>().ToList();
        return JsonSerializer.Serialize(microFiltered);
    }

    [McpServerTool(Name = "delete_draft"), Description("Deletes a draft. Requires both companyId and draftId.")]
    public async Task<string> DeleteDraft(
        [Description("The company's unique identifier")] Guid companyId,
        [Description("The draft's unique identifier")] Guid draftId,
        CancellationToken ct)
    {
        if (IsMonolith)
            await monolithClient.DeleteDraftAsync(companyId, draftId, ct);
        else
            await adminClient.DeleteDraftAsync(companyId, draftId, ct);

        return JsonSerializer.Serialize(new { success = true, draftId });
    }

    [McpServerTool(Name = "drafts_by_company"),
     Description("Returns job drafts grouped by company. Each company entry contains a list of drafts and a count. " +
                 "Only available in monolith mode — returns empty in microservices mode.")]
    public async Task<string> DraftsByCompany(CancellationToken ct)
    {
        if (!IsMonolith)
            return JsonSerializer.Serialize(new { message = "drafts_by_company is only available in monolith mode." });

        var result = await monolithClient.ListAllDraftsByCompanyAsync(ct);
        return JsonSerializer.Serialize(result);
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
