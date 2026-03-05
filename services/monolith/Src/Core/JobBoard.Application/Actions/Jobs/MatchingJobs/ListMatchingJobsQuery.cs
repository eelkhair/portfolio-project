using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Jobs.MatchingJobs;

public class ListMatchingJobsQuery(int limit) : BaseQuery<List<MatchingJobResponse>>
{
    public int Limit { get; } = limit;
}

public class ListMatchingJobsQueryHandler(
    IJobBoardQueryDbContext _,
    IAiServiceClient aiServiceClient,
    IUserAccessor userAccessor,
    IActivityFactory activityFactory,
    ILogger<ListMatchingJobsQueryHandler> logger)
    : BaseQueryHandler(_, logger), IHandler<ListMatchingJobsQuery, List<MatchingJobResponse>>
{
    public async Task<List<MatchingJobResponse>> HandleAsync(ListMatchingJobsQuery request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ListMatchingJobsQueryHandler.HandleAsync", ActivityKind.Internal);
        activity?.SetTag("matching.limit", request.Limit);

        var user = await Context.Users.Where(c=> c.ExternalId == userAccessor.UserId).FirstOrDefaultAsync(cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }
        
        var resume = await Context.Resumes.Where(c=>c.IsDefault && c.UserId == user.InternalId).FirstOrDefaultAsync(cancellationToken);
        if (resume == null)
        {
            throw new NotFoundException("Resume not found");
        }

        activity?.SetTag("resume.uid", resume.Id);

        var similarities = await aiServiceClient.GetMatchingJobsForResumeAsync(resume.Id, request.Limit, cancellationToken);

        var jobIds = similarities.Select(s => s.JobId).ToList();

        var jobs = await Context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .Select(c => new { c.Title, c.AboutRole, c.Id, c.SalaryRange })
            .ToListAsync(cancellationToken);

        var orderedJobs = similarities
            .Join(jobs, s => s.JobId, j => j.Id, (s, j) => new MatchingJobResponse
            {
                JobId = j.Id,
                Title = j.Title,
                AboutRole = j.AboutRole,
                SalaryRange = j.SalaryRange,
                Similarity = s.Similarity
            })
            .OrderByDescending(x => x.Similarity)
            .ToList();

        activity?.SetTag("matching.count", orderedJobs.Count);

        return orderedJobs;
    }
}