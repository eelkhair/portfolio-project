using System.Diagnostics;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Features.Jobs.Create;

public class CreateJobEndpoint(IJobCommandService service) :  Endpoint<EventDto<CreateJobRequest>, JobResponse>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/jobs");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EventDto<CreateJobRequest> request, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "job");
        Activity.Current?.SetTag("operation", "create");

        var isForwardSync = HttpContext.Request.Headers["X-Sync-Source"].FirstOrDefault() == "forward";
        Activity.Current?.SetTag("job.isForwardSync", isForwardSync);
        var response = await service.CreateJobAsync(request.Data, DaprExtensions.CreateUser(request.UserId), ct, publishEvent: !isForwardSync);
        await Send.OkAsync(response, cancellation:ct);
    }
}
