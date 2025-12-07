using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Features.Jobs.Create;

public class CreateJobEndpoint(IJobCommandService service) :  Endpoint<CreateJobRequest, JobResponse>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/jobs");
    }

    public override async Task HandleAsync(CreateJobRequest request, CancellationToken ct)
    {
        var response = await service.CreateJobAsync(request, User, ct);
        await Send.OkAsync(response, cancellation:ct);
    }
}







