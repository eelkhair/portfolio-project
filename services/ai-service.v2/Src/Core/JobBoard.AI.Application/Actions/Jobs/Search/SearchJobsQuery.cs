using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Jobs.Search;

public class SearchJobsQuery(string? query, string? location, string? jobType, int limit = 30) : BaseQuery<List<JobCandidate>>, ISystemCommand
{
    public string? Query { get; } = query;
    public string? Location { get; } = location;
    public string? JobType { get; } = jobType;
    public int Limit { get; } = limit;
}

public partial class SearchJobsQueryHandler(
    IActivityFactory activityFactory,
    IEmbeddingService embeddingService,
    ILogger<SearchJobsQuery> logger,
    IAiDbContext dbContext
    ): BaseQueryHandler(logger),
    IHandler<SearchJobsQuery, List<JobCandidate>>
{
    public async Task<List<JobCandidate>> HandleAsync(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("SearchJobsCommandHandler.HandleAsync", ActivityKind.Internal);
        LogSearchingForJobsWithQueryLocationJobType(Logger, request.Query);

        var searchText = BuildSearchText(request);
        if (string.IsNullOrWhiteSpace(searchText))
            return [];

        var embedding = await embeddingService.GenerateEmbeddingsAsync(searchText, cancellationToken);
        var vectorParam = new Pgvector.Vector(embedding);

        const string sql = """
                           SELECT je."JobId", 1 - (je."VectorData" <=> @embedding) AS "Similarity",
                           ROW_NUMBER() OVER (ORDER BY je."VectorData" <=> @embedding) AS "Rank"
                           FROM job_embeddings je
                           ORDER BY je."VectorData" <=> @embedding
                           LIMIT @limit
                           """;

        var results = await dbContext
            .Set<JobCandidate>()
            .FromSqlRaw(
                sql,
                new Npgsql.NpgsqlParameter("@embedding", vectorParam),
                new Npgsql.NpgsqlParameter("@limit", request.Limit))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return results;
    }

    private static string BuildSearchText(SearchJobsQuery request)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Query))
            parts.Add($"Job Title: {request.Query}");

        if (!string.IsNullOrWhiteSpace(request.Location))
            parts.Add($"Location: {request.Location}");

        if (!string.IsNullOrWhiteSpace(request.JobType))
            parts.Add($"Job Type: {request.JobType}");

        return string.Join("\n", parts);
    }

    [LoggerMessage(LogLevel.Information, "Searching for jobs with query: '{Query}' ")]
    static partial void LogSearchingForJobsWithQueryLocationJobType(ILogger logger, string? Query);
}
