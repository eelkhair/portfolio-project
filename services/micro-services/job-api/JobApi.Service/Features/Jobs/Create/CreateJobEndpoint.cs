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
        var response = await service.CreateJobAsync(request.Data, DaprExtensions.CreateUser(request.UserId), ct);
        await Send.OkAsync(response, cancellation:ct);
    }
}
