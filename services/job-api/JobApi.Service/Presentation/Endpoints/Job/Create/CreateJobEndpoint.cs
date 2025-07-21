using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using FastEndpoints;
using JobApi.Application.Commands.Job;
using JobAPI.Contracts.Job.Requests;
using JobAPI.Contracts.Job.Responses;

namespace JobApi.Presentation.Endpoints.Job.Create;

public class CreateJobEndpoint(Mediator mediator) :  Endpoint<CreateJobRequest, JobResponse, CreateJobMapper>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/jobs");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateJobRequest req, CancellationToken ct)
    {
        var jobId = await mediator.SendAsync(new CreateJobCommand("Senior Dev"), ct);
        await SendAsync(new JobResponse(1, jobId)
      , cancellation:ct);
    }
}





public class Job
{
    public int Id { get; set; }
}

