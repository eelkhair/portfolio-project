using FastEndpoints;
using JobAPI.Contracts.Jobs.Requests;
using JobAPI.Contracts.Jobs.Responses;
using JobApi.Data.Entities;

namespace JobApi.Presentation.Endpoints.Jobs.Create;

public class CreateJobMapper : Mapper<CreateJobRequest, JobResponse, Job>, IRequestMapper, IResponseMapper
{
    
}