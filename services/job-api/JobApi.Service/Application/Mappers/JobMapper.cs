using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Infrastructure.Data.Entities;
using Mapster;

namespace JobApi.Application.Mappers;

public class JobMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Job, JobResponse>();
    }
}