using System.Diagnostics;
using AdminAPI.Contracts.Services;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Elkhair.Dev.Common.Application;
using FastEndpoints;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Features.Jobs.Create;

public class CreateJobEndpoint(IJobCommandService service,
    ActivitySource activitySource) : Endpoint<JobCreateRequest, ApiResponse<JobResponse>>
{
    public override void Configure()
    {
        Post("/jobs");
    }

    public override async Task HandleAsync(JobCreateRequest req, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "job");
        Activity.Current?.SetTag("operation", "create");

        using var act = activitySource.StartActivity(
                     "job.create",
                     ActivityKind.Producer);

        act?.SetTag("job.create.start.sql", req);
        var response = await service.CreateJob(req, ct);
        act?.SetTag("job.create.end.sql", response);
        await Send.OkAsync(response, ct);
    }
}
