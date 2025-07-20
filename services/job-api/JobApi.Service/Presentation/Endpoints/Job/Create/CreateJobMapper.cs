using FastEndpoints;
using JobAPI.Contracts.Job.Requests;
using JobAPI.Contracts.Job.Responses;

namespace JobApi.Presentation.Endpoints.Job.Create;

public class CreateJobMapper : Mapper<CreateJobRequest, JobResponse, Presentation.Endpoints.Job.Create.Job>, IRequestMapper, IResponseMapper
{
    
}