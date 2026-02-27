using System.Diagnostics;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.IntegrationEvents.Job;
using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Application.Actions.Jobs.Create;

public static class CreateJobMappers
{
    public static void SetActivityTagsForJob(this CreateJobRequest request, Activity? activity)
    {
        activity?.SetTag("CompanyUId", request.CompanyUId.ToString());
        activity?.SetTag("JobType", request.JobType.ToString());
        activity?.SetTag("JobTitle", request.Title);
        activity?.SetTag("Location", request.Location);
    }

    public static Job ToJobEntity(this CreateJobRequest request, Guid uid, int id, int companyId)
    {
        return Job.Create(new JobInput
        {
            AboutRole = request.AboutRole,
            JobType = request.JobType,
            Title = request.Title,
            Location = request.Location,
            Qualifications = request.Qualifications,
            Responsibilities = request.Responsibilities,
            SalaryRange = request.SalaryRange,
            CompanyId = companyId,
            InternalId = id,
            UId = uid,
        });
    }

    public static JobCreatedV1Event ToIntegrationEvent(this CreateJobRequest request, Guid uid)
    {
        return new JobCreatedV1Event(
            UId: uid,
            CompanyUId: request.CompanyUId,
            Title: request.Title,
            AboutRole: request.AboutRole,
            Location: request.Location,
            SalaryRange: request.SalaryRange,
            DraftId: request.DraftId,
            DeleteDraft: request.DeleteDraft,
            Responsibilities: request.Responsibilities,
            Qualifications: request.Qualifications,
            JobType: request.JobType.ToString()
        );
    }
}