using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elkhair.Dev.Common.Dapr;
using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobAPI.Contracts.Models.Drafts.Requests;
using JobAPI.Contracts.Models.Drafts.Responses;
using JobBoard.IntegrationEvents.Draft;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public partial class DraftCommandService(IJobDbContext context, IMessageSender messageSender, ILogger<DraftCommandService> logger) : IDraftCommandService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DraftResponse> SaveDraftAsync(Guid companyUId, SaveDraftRequest request, ClaimsPrincipal user, CancellationToken ct, bool publishEvent = true)
    {
        LogSavingDraft(logger, companyUId, request.Id);
        Activity.Current?.SetTag("draft.incoming.id", request.Id);
        Activity.Current?.SetTag("draft.companyUid", companyUId);
        Activity.Current?.SetTag("draft.publishEvent", publishEvent);

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

        // Publish reverse-sync event (skipped for forward-sync to prevent loops)
        if (publishEvent)
        {
            var userId = user.FindFirst("sub")?.Value ?? "system";
            var evt = new DraftSavedV1Event(
                draft.UId, companyUId, request.Title, request.AboutRole,
                request.Location, request.JobType, request.SalaryRange, request.Notes,
                request.Responsibilities, request.Qualifications);
            await messageSender.SendEventAsync("rabbitmq.pubsub", "micro.draft-saved.v1", userId, evt, ct);
        }

        var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOpts) ?? new DraftResponse();
        response.Id = draft.UId.ToString();

        LogDraftSaved(logger, draft.UId);
        return response;
    }

    public async Task DeleteDraftAsync(Guid draftUId, ClaimsPrincipal user, CancellationToken ct, bool publishEvent = true)
    {
        LogDeletingDraft(logger, draftUId);
        Activity.Current?.SetTag("draft.uid", draftUId);
        Activity.Current?.SetTag("draft.publishEvent", publishEvent);

        var draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == draftUId, ct)
                    ?? throw new InvalidOperationException($"Draft '{draftUId}' not found.");

        // Capture company UId before removal for the event
        var companyUId = await context.Companies
            .Where(c => c.Id == draft.CompanyId)
            .Select(c => c.UId)
            .FirstAsync(ct);

        context.Drafts.Remove(draft);
        await context.SaveChangesAsync(user, ct);

        // Publish reverse-sync event (skipped for forward-sync to prevent loops)
        if (publishEvent)
        {
            var userId = user.FindFirst("sub")?.Value ?? "system";
            var evt = new DraftDeletedV1Event(draftUId, companyUId);
            await messageSender.SendEventAsync("rabbitmq.pubsub", "micro.draft-deleted.v1", userId, evt, ct);
        }

        LogDraftDeleted(logger, draftUId);
    }

    [LoggerMessage(LogLevel.Information, "Saving draft for company {CompanyUId}, incoming ID '{DraftId}'")]
    static partial void LogSavingDraft(ILogger logger, Guid companyUId, string? draftId);

    [LoggerMessage(LogLevel.Information, "Draft saved: {DraftUId}")]
    static partial void LogDraftSaved(ILogger logger, Guid draftUId);

    [LoggerMessage(LogLevel.Information, "Deleting draft {DraftUId}")]
    static partial void LogDeletingDraft(ILogger logger, Guid draftUId);

    [LoggerMessage(LogLevel.Information, "Draft deleted: {DraftUId}")]
    static partial void LogDraftDeleted(ILogger logger, Guid draftUId);
}
