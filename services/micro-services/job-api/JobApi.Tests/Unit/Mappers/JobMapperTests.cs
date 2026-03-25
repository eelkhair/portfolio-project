using JobApi.Infrastructure.Data.Entities;
using JobApi.Tests.Helpers;
using JobAPI.Contracts.Enums;
using JobAPI.Contracts.Models.Jobs.Requests;
using Mapster;
using Shouldly;

using JobResponse = JobAPI.Contracts.Models.Jobs.Responses.JobResponse;

namespace JobApi.Tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class JobMapperTests
{
    public JobMapperTests()
    {
        MapsterSetup.Initialize();
    }

    // ── Job → JobResponse ──

    [Fact]
    public void Job_ToJobResponse_ShouldMapQualificationsToStringList()
    {
        var job = CreateJob();
        job.Qualifications = new List<Qualification>
        {
            new() { Value = "C#" },
            new() { Value = ".NET" },
            new() { Value = "SQL" }
        };

        var response = job.Adapt<JobResponse>();

        response.Qualifications.Count.ShouldBe(3);
        response.Qualifications.ShouldContain("C#");
        response.Qualifications.ShouldContain(".NET");
        response.Qualifications.ShouldContain("SQL");
    }

    [Fact]
    public void Job_ToJobResponse_ShouldMapResponsibilitiesToStringList()
    {
        var job = CreateJob();
        job.Responsibilities = new List<Responsibility>
        {
            new() { Value = "Design systems" },
            new() { Value = "Code reviews" }
        };

        var response = job.Adapt<JobResponse>();

        response.Responsibilities.Count.ShouldBe(2);
        response.Responsibilities.ShouldContain("Design systems");
        response.Responsibilities.ShouldContain("Code reviews");
    }

    [Fact]
    public void Job_ToJobResponse_ShouldMapScalarFields()
    {
        var uid = Guid.NewGuid();
        var job = new Job
        {
            UId = uid,
            Title = "Senior Engineer",
            Location = "Remote",
            JobType = JobType.FullTime,
            AboutRole = "Lead backend development",
            SalaryRange = "$120k-$150k",
            CompanyId = 1,
            Company = new Company { Name = "Acme", UId = Guid.NewGuid() },
            Responsibilities = [],
            Qualifications = []
        };

        var response = job.Adapt<JobResponse>();

        response.UId.ShouldBe(uid);
        response.Title.ShouldBe("Senior Engineer");
        response.Location.ShouldBe("Remote");
        response.AboutRole.ShouldBe("Lead backend development");
        response.SalaryRange.ShouldBe("$120k-$150k");
    }

    [Fact]
    public void Job_ToJobResponse_ShouldHandleEmptyCollections()
    {
        var job = CreateJob();
        job.Responsibilities = [];
        job.Qualifications = [];

        var response = job.Adapt<JobResponse>();

        response.Responsibilities.ShouldBeEmpty();
        response.Qualifications.ShouldBeEmpty();
    }

    // ── CreateJobRequest → Job ──

    [Fact]
    public void CreateJobRequest_ToJob_ShouldMapResponsibilitiesToEntityList()
    {
        var request = new CreateJobRequest
        {
            Title = "Engineer",
            Location = "NYC",
            JobType = JobType.FullTime,
            AboutRole = "Build stuff",
            Responsibilities = new List<string> { "Design", "Code" },
            Qualifications = new List<string> { "C#" }
        };

        var job = request.Adapt<Job>();

        job.Responsibilities.Count.ShouldBe(2);
        job.Responsibilities.ShouldAllBe(r => !string.IsNullOrEmpty(r.Value));
    }

    [Fact]
    public void CreateJobRequest_ToJob_ShouldMapQualificationsToEntityList()
    {
        var request = new CreateJobRequest
        {
            Title = "Engineer",
            Location = "NYC",
            JobType = JobType.FullTime,
            AboutRole = "Build stuff",
            Responsibilities = new List<string> { "Design" },
            Qualifications = new List<string> { "C#", ".NET", "SQL" }
        };

        var job = request.Adapt<Job>();

        job.Qualifications.Count.ShouldBe(3);
        job.Qualifications.Select(q => q.Value).ShouldContain("C#");
        job.Qualifications.Select(q => q.Value).ShouldContain(".NET");
        job.Qualifications.Select(q => q.Value).ShouldContain("SQL");
    }

    [Fact]
    public void CreateJobRequest_ToJob_ShouldHandleNullResponsibilities()
    {
        var request = new CreateJobRequest
        {
            Title = "Engineer",
            Location = "NYC",
            JobType = JobType.FullTime,
            AboutRole = "Build stuff",
            Responsibilities = null!,
            Qualifications = new List<string>()
        };

        var job = request.Adapt<Job>();

        job.Responsibilities.ShouldBeEmpty();
    }

    [Fact]
    public void CreateJobRequest_ToJob_ShouldHandleNullQualifications()
    {
        var request = new CreateJobRequest
        {
            Title = "Engineer",
            Location = "NYC",
            JobType = JobType.FullTime,
            AboutRole = "Build stuff",
            Responsibilities = new List<string>(),
            Qualifications = null!
        };

        var job = request.Adapt<Job>();

        job.Qualifications.ShouldBeEmpty();
    }

    [Fact]
    public void CreateJobRequest_ToJob_ShouldMapTitle()
    {
        var request = new CreateJobRequest
        {
            Title = "Staff Engineer",
            Location = "Remote",
            JobType = JobType.Contract,
            AboutRole = "Architect systems",
            Responsibilities = new List<string>(),
            Qualifications = new List<string>()
        };

        var job = request.Adapt<Job>();

        job.Title.ShouldBe("Staff Engineer");
        job.Location.ShouldBe("Remote");
        job.JobType.ShouldBe(JobType.Contract);
        job.AboutRole.ShouldBe("Architect systems");
    }

    private static Job CreateJob() => new()
    {
        Title = "Engineer",
        Location = "Remote",
        JobType = JobType.FullTime,
        AboutRole = "Build software",
        CompanyId = 1,
        Company = new Company { Name = "Test Co", UId = Guid.NewGuid() },
        Responsibilities = [],
        Qualifications = []
    };
}
