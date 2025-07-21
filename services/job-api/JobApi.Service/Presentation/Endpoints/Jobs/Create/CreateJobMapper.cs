using FastEndpoints;
using JobAPI.Contracts.Job.Responses;
using JobAPI.Contracts.Jobs.Requests;
using JobApi.Data.Entities;

namespace JobApi.Presentation.Endpoints.Jobs.Create;

public class CreateJobMapper : Mapper<CreateJobRequest, JobResponse, Job>, IRequestMapper, IResponseMapper
{
    
}