using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Settings.ReEmbedJobs;

public class ReEmbedAllJobsCommand : BaseCommand<ReEmbedAllJobsResponse>, INoTransaction;

public class ReEmbedAllJobsCommandHandler(
    IHandlerContext handlerContext,
    IAiServiceClient aiServiceClient)
    : BaseCommandHandler(handlerContext), IHandler<ReEmbedAllJobsCommand, ReEmbedAllJobsResponse>
{
    public async Task<ReEmbedAllJobsResponse> HandleAsync(
        ReEmbedAllJobsCommand command,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("operation", "re-embed-all-jobs");
        Logger.LogInformation("Requesting AI service to re-embed all jobs...");

        var result = await aiServiceClient.ReEmbedAllJobs(cancellationToken);

        Logger.LogInformation("AI service re-embedded {Count} jobs", result.JobsProcessed);
        return result;
    }
}
