using System.Diagnostics;
using AdminAPI.Contracts.Services;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.Rewrite;

public class RewriteEndpoint(IJobCommandService jobCommandService, ILogger<RewriteEndpoint> logger):
    Endpoint<JobRewriteRequest,ApiResponse<JobRewriteResponse>>
{
    public override void Configure()
    {
        Put("/jobs/drafts/rewrite");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Rewrite Item";
            s.Description = "Rewrites a job.";
        });
    }

    public override async Task HandleAsync(JobRewriteRequest req, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "draft");
        Activity.Current?.SetTag("operation", "update");
        logger.LogInformation("rewriting item {@Request}", req);
        var response = await jobCommandService.RewriteItem(req, ct);
        await Send.OkAsync(response, ct);
    }
}