using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.SimilarJobs;

public class GetSimilarJobsQuery(Guid jobId, int limit = 5)
    : BaseQuery<List<SimilarJobCandidate>>, ISystemCommand
{
    public Guid JobId { get; } = jobId;
    public int Limit { get; } = limit;
}

public class GetSimilarJobsQueryHandler(
    ILogger<GetSimilarJobsQuery> logger,
    IAiDbContext context)
    : BaseQueryHandler(logger),
        IHandler<GetSimilarJobsQuery, List<SimilarJobCandidate>>
{
    public async Task<List<SimilarJobCandidate>> HandleAsync(
        GetSimilarJobsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Finding similar jobs for JobId {JobId} with limit {Limit}",
            request.JobId,
            request.Limit);

        const string sql = """
                           SELECT
                               je."JobId",
                               1 - (je."VectorData" <=> q."VectorData") AS "Similarity",
                               ROW_NUMBER() OVER (ORDER BY je."VectorData" <=> q."VectorData") AS "Rank"
                           FROM job_embeddings je
                           JOIN job_embeddings q
                             ON q."JobId" = @jobId
                           WHERE je."JobId" <> @jobId
                           LIMIT @limit;
                           """;

        var results = await context
            .Set<SimilarJobCandidate>()
            .FromSqlRaw(
                sql,
                new Npgsql.NpgsqlParameter("jobId", request.JobId),
                new Npgsql.NpgsqlParameter("limit", request.Limit))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Assign ranks deterministically
        for (var i = 0; i < results.Count; i++)
        {
            results[i].Rank = i + 1;
        }

        return results;
    }
}