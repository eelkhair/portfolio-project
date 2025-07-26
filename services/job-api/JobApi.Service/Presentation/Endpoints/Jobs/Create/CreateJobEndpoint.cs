using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using FastEndpoints;
using JobApi.Application.Commands.Jobs;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Presentation.Endpoints.Jobs.Create;

public class CreateJobEndpoint(IMediator mediator, IRequestFactory factory) :  Endpoint<CreateJobRequest, JobResponse, CreateJobMapper>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/jobs");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateJobRequest request, CancellationToken ct)
    {
        var job = Map.ToEntity(request);
        var command = factory.Create<CreateJobCommand>(job, request.CompanyUId);
        var response = await mediator.SendAsync(command, ct);
        await SendAsync(Map.FromEntity(response)
      , cancellation:ct);
    }
}







