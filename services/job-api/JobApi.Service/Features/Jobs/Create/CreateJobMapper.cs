using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Responses;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Data.Entities;

namespace JobApi.Presentation.Endpoints.Jobs.Create;

public class CreateJobMapper : Mapper<CreateJobRequest, JobResponse, Job>
{
    public override Job ToEntity(CreateJobRequest src)
    {
       return new()
            {
                Title = src.Title,
                Location = src.Location,
                JobType = src.JobType,
                AboutRole = src.AboutRole,
                SalaryRange = src.SalaryRange,
                Responsibilities = src.Responsibilities.Select(c=> new Responsibility {Value = c}).ToList(),
                Qualifications = src.Qualifications.Select(c=> new Qualification {Value = c}).ToList()
            };
    }

    public override JobResponse FromEntity(Job e) => new()
    {
        UId = e.UId,
        Title = e.Title,
        Location = e.Location,
        JobType = e.JobType,
        AboutRole = e.AboutRole,
        SalaryRange = e.SalaryRange,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Responsibilities = e.Responsibilities.Select(c=> c.Value).ToList(),
        Qualifications = e.Qualifications.Select(c=> c.Value).ToList(),
        Company = e.Company == null? null : new CompanyResponse
        {
            UId = e.Company.UId,
            Name = e.Company.Name,
            About = e.Company.About,
            EEO = e.Company.EEO,
            CreatedAt = e.Company.CreatedAt,
            UpdatedAt = e.Company.UpdatedAt,
        }

    };

}