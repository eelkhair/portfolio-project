using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Infrastructure.Data.Entities;
using Mapster;

namespace JobApi.Application.Mappers;

public class JobMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Job, JobResponse>()
            .Map(d=> d.Qualifications, s=> s.Qualifications.Select(v=>v.Value).ToList())
            .Map(d=> d.Responsibilities, s=> s.Responsibilities.Select(v=>v.Value).ToList());
        config.NewConfig<CreateJobRequest, Job>()
            // ⚠️ destination must be a direct member; project on the source side
            .Map(d => d.Responsibilities,
                s => (s.Responsibilities ?? new())
                    .Select(v => new Responsibility { Value = v })
                    .ToList())

            .Map(d => d.Qualifications,
                s => (s.Qualifications ?? new())
                    .Select(v => new Qualification { Value = v })
                    .ToList())

            .Ignore(d => d.CompanyId)
            .Ignore(d => d.Company);
    }
}