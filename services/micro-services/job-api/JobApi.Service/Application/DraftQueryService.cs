using System.Text.Json;
using System.Text.Json.Serialization;
using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobAPI.Contracts.Models.Drafts.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobApi.Application;

public partial class DraftQueryService(IJobDbContext context, ILogger<DraftQueryService> logger) : IDraftQueryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<List<DraftResponse>> ListDraftsAsync(Guid companyUId, CancellationToken ct)
    {
        LogListingDrafts(logger, companyUId);

        if (companyUId == Guid.Empty) return [];

        var company = await context.Companies.FirstOrDefaultAsync(c => c.UId == companyUId, ct);
        if (company is null) return [];

        var drafts = await context.Drafts
            .Where(d => d.CompanyId == company.Id)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);

        var result = drafts.Select(d =>
        {
            var response = JsonSerializer.Deserialize<DraftResponse>(d.ContentJson, JsonOpts) ?? new DraftResponse();
            response.Id = d.UId.ToString();
            return response;
        }).ToList();

        LogDraftsListed(logger, companyUId, result.Count);
        return result;
    }

    public async Task<DraftResponse?> GetDraftAsync(Guid draftUId, CancellationToken ct)
    {
        LogGettingDraft(logger, draftUId);

        var draft = await context.Drafts.FirstOrDefaultAsync(d => d.UId == draftUId, ct);
        if (draft is null)
        {
            LogDraftNotFound(logger, draftUId);
            return null;
        }

        var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOpts) ?? new DraftResponse();
        response.Id = draft.UId.ToString();

        LogDraftRetrieved(logger, draftUId);
        return response;
    }

    [LoggerMessage(LogLevel.Information, "Listing drafts for company {CompanyUId}")]
    static partial void LogListingDrafts(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Listed {Count} drafts for company {CompanyUId}")]
    static partial void LogDraftsListed(ILogger logger, Guid companyUId, int count);

    [LoggerMessage(LogLevel.Information, "Getting draft {DraftUId}")]
    static partial void LogGettingDraft(ILogger logger, Guid draftUId);

    [LoggerMessage(LogLevel.Information, "Draft {DraftUId} not found")]
    static partial void LogDraftNotFound(ILogger logger, Guid draftUId);

    [LoggerMessage(LogLevel.Information, "Draft retrieved: {DraftUId}")]
    static partial void LogDraftRetrieved(ILogger logger, Guid draftUId);
}
