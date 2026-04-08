using System.Text.Json;
using System.Text.Json.Serialization;
using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobAPI.Contracts.Models.Drafts.Responses;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public class DraftQueryService(IJobDbContext context) : IDraftQueryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<List<DraftResponse>> ListDraftsAsync(Guid companyUId, CancellationToken ct)
    {
        if (companyUId == Guid.Empty) return [];

        var company = await context.Companies.FirstOrDefaultAsync(c => c.UId == companyUId, ct);
        if (company is null) return [];

        var drafts = await context.Drafts
            .Where(d => d.CompanyId == company.Id)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);

        return drafts.Select(d =>
        {
            var response = JsonSerializer.Deserialize<DraftResponse>(d.ContentJson, JsonOpts) ?? new DraftResponse();
            response.Id = d.UId.ToString();
            return response;
        }).ToList();
    }

    public async Task<DraftResponse?> GetDraftAsync(Guid draftUId, CancellationToken ct)
    {
        var draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == draftUId, ct);
        if (draft is null) return null;

        var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOpts) ?? new DraftResponse();
        response.Id = draft.UId.ToString();
        return response;
    }
}
