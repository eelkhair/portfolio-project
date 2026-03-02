using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Common;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class ListPublicJobsQuery : BaseQuery<PaginatedList<JobResponse>>, IAnonymousRequest
{
    public string? Search { get; set; }
    public JobType? JobType { get; set; }
    public string? Location { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ListPublicJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<ListPublicJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListPublicJobsQuery, PaginatedList<JobResponse>>
{
    public async Task<PaginatedList<JobResponse>> HandleAsync(ListPublicJobsQuery request, CancellationToken cancellationToken)
    {
        var query = Context.Jobs.OrderByDescending(j => j.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(job => new JobResponse
            {
                Id = job.Id,
                CompanyUId = job.Company.Id,
                Title = job.Title,
                JobType = job.JobType,
                Location = job.Location,
                CompanyName = job.Company.Name,
                AboutRole = job.AboutRole,
                SalaryRange = job.SalaryRange,
                Responsibilities = job.Responsibilities.Select(r => r.Value).ToList(),
                Qualifications = job.Qualifications.Select(q => q.Value).ToList(),
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<JobResponse>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.Page > 1,
            HasNextPage = request.Page < totalPages
        };
    }
}
