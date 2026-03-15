using System.Diagnostics;
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
        Activity.Current?.SetTag("draft.incoming.id", request.Id);
        Activity.Current?.SetTag("draft.companyUid", companyUId);

        var company = await context.Companies.FirstAsync(c => c.UId == companyUId, ct);
        var contentJson = JsonSerializer.Serialize(request, JsonOpts);

        Draft? draft = null;
        if (!string.IsNullOrWhiteSpace(request.Id) && Guid.TryParse(request.Id, out var existingUId))
        {
            draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == existingUId && d.CompanyId == company.Id, ct);
        }

        if (draft is not null)
        {
            Activity.Current?.SetTag("draft.isNew", false);
            draft.ContentJson = contentJson;
            draft.DraftStatus = "generated";
        }
        else
        {
            Activity.Current?.SetTag("draft.isNew", true);
            draft = new Draft
            {
                CompanyId = company.Id,
                DraftType = "job",
                DraftStatus = "generated",
                ContentJson = contentJson
            };
            context.Drafts.Add(draft);

            // Preserve the monolith's UId — set AFTER Add() and mark non-temporary
            // to override ValueGeneratedOnAdd from HasDefaultValueSql("newsequentialid()")
            if (!string.IsNullOrWhiteSpace(request.Id) && Guid.TryParse(request.Id, out var providedUId))
            {
                draft.UId = providedUId;
                ((DbContext)context).Entry(draft).Property(e => e.UId).IsTemporary = false;
                Activity.Current?.SetTag("draft.providedUId", providedUId);
            }
        }

        await context.SaveChangesAsync(user, ct);

        Activity.Current?.SetTag("draft.final.uid", draft.UId);

        var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOpts) ?? new DraftResponse();
        response.Id = draft.UId.ToString();
        return response;
    }

    public async Task DeleteDraftAsync(Guid draftUId, ClaimsPrincipal user, CancellationToken ct)
    {
        Activity.Current?.SetTag("draft.uid", draftUId);

        var draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == draftUId, ct)
                    ?? throw new InvalidOperationException($"Draft '{draftUId}' not found.");

        context.Drafts.Remove(draft);
        await context.SaveChangesAsync(user, ct);
    }
}
