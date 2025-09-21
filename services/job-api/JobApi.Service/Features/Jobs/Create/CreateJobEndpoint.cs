using FastEndpoints;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Features.Jobs.Create;

public class CreateJobEndpoint :  Endpoint<CreateJobRequest, JobResponse>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/jobs");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateJobRequest request, CancellationToken ct)
    {
   
        await SendAsync(new JobResponse()
      , cancellation:ct);
    }
}







