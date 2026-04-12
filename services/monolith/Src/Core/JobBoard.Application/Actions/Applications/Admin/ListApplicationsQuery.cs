using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
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
    int pageSize = 50) : BaseQuery<PaginatedResponse<AdminApplicationListItem>>
{
    public string? Status { get; } = status;
    public Guid? JobId { get; } = jobId;
    public string? Search { get; } = search;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
}

public class ListApplicationsQueryHandler(
    IJobBoardQueryDbContext context,
    ILogger<ListApplicationsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListApplicationsQuery, PaginatedResponse<AdminApplicationListItem>>
{
    public async Task<PaginatedResponse<AdminApplicationListItem>> HandleAsync(
        ListApplicationsQuery query, CancellationToken cancellationToken)
    {
        var q = Context.JobApplications
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .Include(a => a.User)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrEmpty(query.Status) &&
            Enum.TryParse<ApplicationStatus>(query.Status, true, out var statusEnum))
        {
            q = q.Where(a => a.Status == statusEnum);
        }

        // Filter by job
        if (query.JobId.HasValue)
        {
            q = q.Where(a => a.Job.Id == query.JobId.Value);
        }

        // Search by applicant name or email
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(a =>
                a.User.FirstName.ToLower().Contains(term) ||
                a.User.LastName.ToLower().Contains(term) ||
                a.User.Email.ToLower().Contains(term));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AdminApplicationListItem
            {
                Id = a.Id,
                ApplicantName = $"{a.User.FirstName} {a.User.LastName}".Trim(),
                ApplicantEmail = a.User.Email,
                JobId = a.Job.Id,
                JobTitle = a.Job.Title,
                CompanyName = a.Job.Company.Name,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AdminApplicationListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
