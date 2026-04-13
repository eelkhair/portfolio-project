using System.Diagnostics;
using AdminAPI.Contracts.Models.Settings;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Settings.ReEmbedJobs;

public sealed class ReEmbedJobsEndpoint(ISettingsCommandService settings)
    : EndpointWithoutRequest<ApiResponse<ReEmbedJobsResponse>>
{
    private const string RouteTemplate = "settings/re-embed-jobs";

    public override void Configure()
    {
        Post(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Re-embed all jobs";
            s.Description = "Regenerates vector embeddings for all jobs using the current AI provider.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "settings");
        Activity.Current?.SetTag("operation", "publish");
        var result = await settings.ReEmbedJobsAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
