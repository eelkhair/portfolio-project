using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector.EntityFrameworkCore;


namespace JobBoard.AI.Application.Actions.Resumes.MatchingJobs;

public class ListMatchingJobsQuery(Guid resumeId, int limit = 10): BaseQuery<List<JobCandidate>> , ISystemCommand
{
    public int Limit { get; } = limit;
    public Guid ResumeId { get; } = resumeId;
}


public class ListMatchingJobsQueryHandler(IAiDbContext context,
    ILogger<ListMatchingJobsQueryHandler> logger
) : BaseQueryHandler(logger)
, IHandler<ListMatchingJobsQuery, List<JobCandidate>>
{
    public async Task<List<JobCandidate>> HandleAsync(ListMatchingJobsQuery request, CancellationToken cancellationToken)
    {
        var resumeVector = await context.ResumeEmbeddings
            .Where(e => e.ResumeUId == request.ResumeId)
            .Select(e => e.VectorData)
            .FirstOrDefaultAsync(cancellationToken);

        if (resumeVector == null)
        {
            throw new InvalidOperationException($"Resume with ID {request.ResumeId} does not have an embedding.");
        }

        var similarities = await context.JobEmbeddings
            .OrderByDescending(e =>  1 - e.VectorData.CosineDistance(resumeVector))
            .Select(c => new JobCandidate
            {
                JobId = c.JobId,
                Similarity = 1 - c.VectorData.CosineDistance(resumeVector),
            })
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var i = 1;
        foreach (var item in similarities)
        {
            item.Rank = i++;
        }
        return similarities;
        
    }
}