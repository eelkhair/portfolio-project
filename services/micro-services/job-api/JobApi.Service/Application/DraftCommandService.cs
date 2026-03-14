using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobAPI.Contracts.Models.Drafts.Requests;
using JobAPI.Contracts.Models.Drafts.Responses;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public class DraftCommandService(IJobDbContext context) : IDraftCommandService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DraftResponse> SaveDraftAsync(Guid companyUId, SaveDraftRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var company = await context.Companies.FirstAsync(c => c.UId == companyUId, ct);
        var contentJson = JsonSerializer.Serialize(request, JsonOpts);

        Draft? draft = null;
        if (!string.IsNullOrWhiteSpace(request.Id) && Guid.TryParse(request.Id, out var existingUId))
        {
            draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == existingUId && d.CompanyId == company.Id, ct);
        }

        if (draft is not null)
        {
            draft.ContentJson = contentJson;
            draft.DraftStatus = "generated";
        }
        else
        {
            draft = new Draft
            {
                CompanyId = company.Id,
                DraftType = "job",
                DraftStatus = "generated",
                ContentJson = contentJson
            };
            context.Drafts.Add(draft);
        }

        await context.SaveChangesAsync(user, ct);

        var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOpts) ?? new DraftResponse();
        response.Id = draft.UId.ToString();
        return response;
    }

    public async Task DeleteDraftAsync(Guid draftUId, ClaimsPrincipal user, CancellationToken ct)
    {
        var draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == draftUId, ct)
                    ?? throw new InvalidOperationException($"Draft '{draftUId}' not found.");

        context.Drafts.Remove(draft);
        await context.SaveChangesAsync(user, ct);
    }
}
