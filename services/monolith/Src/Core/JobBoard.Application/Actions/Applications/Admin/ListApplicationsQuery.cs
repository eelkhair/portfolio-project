using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Applications.Admin;

public class ListApplicationsQuery(
    string? status = null,
    Guid? jobId = null,
    string? search = null,
    int page = 1,
    int pageSize = 50,
    bool includeMatchScores = false) : BaseQuery<PaginatedResponse<AdminApplicationListItem>>
{
    public string? Status { get; } = status;
    public Guid? JobId { get; } = jobId;
    public string? Search { get; } = search;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public bool IncludeMatchScores { get; } = includeMatchScores;
}

public class ListApplicationsQueryHandler(
    IJobBoardQueryDbContext context,
    IAiServiceClient aiServiceClient,
    ILogger<ListApplicationsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListApplicationsQuery, PaginatedResponse<AdminApplicationListItem>>
{
    private record AppProjection(
        Guid Id, string ApplicantName, string Email, Guid JobId,
        string Title, string CompanyName, string Status,
        DateTime CreatedAt, DateTime? UpdatedAt, Guid? ResumeUId);

    public async Task<PaginatedResponse<AdminApplicationListItem>> HandleAsync(
        ListApplicationsQuery query, CancellationToken cancellationToken)
    {
        var q = Context.JobApplications
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .Include(a => a.User)
            .Include(a => a.Resume)
            .AsQueryable();

        if (!string.IsNullOrEmpty(query.Status) &&
            Enum.TryParse<ApplicationStatus>(query.Status, true, out var statusEnum))
            q = q.Where(a => a.Status == statusEnum);

        if (query.JobId.HasValue)
            q = q.Where(a => a.Job.Id == query.JobId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(a =>
                a.User.FirstName.ToLower().Contains(term) ||
                a.User.LastName.ToLower().Contains(term) ||
                a.User.Email.ToLower().Contains(term));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var projected = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AppProjection(
                a.Id,
                (a.User.FirstName + " " + a.User.LastName).Trim(),
                a.User.Email,
                a.Job.Id,
                a.Job.Title,
                a.Job.Company.Name,
                a.Status.ToString(),
                a.CreatedAt,
                a.UpdatedAt,
                a.Resume != null ? a.Resume.Id : (Guid?)null))
            .ToListAsync(cancellationToken);

        var items = projected.Select(a => new AdminApplicationListItem
        {
            Id = a.Id,
            ApplicantName = a.ApplicantName,
            ApplicantEmail = a.Email,
            JobId = a.JobId,
            JobTitle = a.Title,
            CompanyName = a.CompanyName,
            Status = a.Status,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();

        if (query.IncludeMatchScores)
            await EnrichWithMatchScores(projected, items, cancellationToken);

        return new PaginatedResponse<AdminApplicationListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    private async Task EnrichWithMatchScores(
        List<AppProjection> projected,
        List<AdminApplicationListItem> items,
        CancellationToken ct)
    {
        var resumeGroups = projected
            .Where(a => a.ResumeUId.HasValue)
            .GroupBy(a => a.ResumeUId!.Value)
            .ToList();

        foreach (var group in resumeGroups)
        {
            try
            {
                var matches = await aiServiceClient.GetMatchingJobsForResumeAsync(group.Key, 50, ct);
                var matchMap = matches.ToDictionary(m => m.JobId);

                foreach (var app in group)
                {
                    var item = items.FirstOrDefault(r => r.Id == app.Id);
                    if (item != null && matchMap.TryGetValue(app.JobId, out var match))
                    {
                        item.MatchScore = Math.Round(match.Similarity * 100);
                        item.MatchSummary = match.MatchSummary;
                        item.MatchDetails = match.MatchDetails;
                        item.MatchGaps = match.MatchGaps;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch match scores for resume {ResumeId}", group.Key);
            }
        }
    }
}
