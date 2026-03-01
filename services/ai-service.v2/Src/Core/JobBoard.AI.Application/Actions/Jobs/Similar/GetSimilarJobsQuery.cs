using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Jobs.Similar;

public class GetSimilarJobsQuery(Guid jobId, int limit = 5)
    : BaseQuery<List<JobCandidate>>, ISystemCommand
{
    public Guid JobId { get; } = jobId;
    public int Limit { get; } = limit;
}

public partial class GetSimilarJobsQueryHandler(
    ILogger<GetSimilarJobsQuery> logger,
    IActivityFactory activityFactory,
    IAiDbContext context)
    : BaseQueryHandler(logger),
        IHandler<GetSimilarJobsQuery, List<JobCandidate>>
{
    public async Task<List<JobCandidate>> HandleAsync(
        GetSimilarJobsQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("GetSimilarJobsQueryHandler.HandleAsync", ActivityKind.Internal);
        LogFindingSimilarJobsForJobIdWithLimit(Logger, request.JobId, request.Limit);

        const string sql = """
                           SELECT
                               je."JobId",
                               1 - (je."VectorData" <=> q."VectorData") AS "Similarity",
                               ROW_NUMBER() OVER (ORDER BY je."VectorData" <=> q."VectorData") AS "Rank"
                           FROM job_embeddings je
                           JOIN job_embeddings q
                             ON q."JobId" = @jobId
                           WHERE je."JobId" <> @jobId
                           ORDER BY je."VectorData" <=> q."VectorData"
                           LIMIT @limit;
                           """;

        var results = await context
            .Set<JobCandidate>()
            .FromSqlRaw(
                sql,
                new Npgsql.NpgsqlParameter("jobId", request.JobId),
                new Npgsql.NpgsqlParameter("limit", request.Limit))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        activity?.SetTag("similarJobsCount", results.Count);
        // Assign ranks deterministically
        for (var i = 0; i < results.Count; i++)
        {
            results[i].Rank = i + 1;
        }

        return results;
    }

    [LoggerMessage(LogLevel.Information, "Finding similar jobs for JobId {JobId} with limit {Limit}")]
    static partial void LogFindingSimilarJobsForJobIdWithLimit(ILogger logger, Guid JobId, int Limit);
}