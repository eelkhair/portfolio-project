using Shouldly;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;
using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Monolith.Tests.Unit.Domain.Jobs;

[Trait("Category", "Unit")]
public class JobAggregateTests
{
    private static JobInput CreateValidInput(
        string title = "Software Engineer",
        string location = "Remote",
        string aboutRole = "Build great software",
        JobType jobType = JobType.FullTime,
        int companyId = 1) =>
        new()
        {
            Title = title,
            Location = location,
            AboutRole = aboutRole,
            JobType = jobType,
            CompanyId = companyId
        };

    [Fact]
    public void Create_WithValidInput_ShouldReturnJob()
    {
        var input = CreateValidInput();

        var job = Job.Create(input);

        job.Title.ShouldBe("Software Engineer");
        job.Location.ShouldBe("Remote");
        job.AboutRole.ShouldBe("Build great software");
        job.JobType.ShouldBe(JobType.FullTime);
        job.CompanyId.ShouldBe(1);
    }

    [Fact]
    public void Create_WithSalaryRange_ShouldSetValue()
    {
        var input = CreateValidInput();
        input.SalaryRange = "$100k - $150k";

        var job = Job.Create(input);

        job.SalaryRange.ShouldBe("$100k - $150k");
    }

    [Fact]
    public void Create_WithResponsibilities_ShouldAddAll()
    {
        var input = CreateValidInput();
        input.Responsibilities = ["Design APIs", "Code review", "Mentoring"];

        var job = Job.Create(input);

        job.Responsibilities.Count().ShouldBe(3);
        var values = job.Responsibilities.Select(r => r.Value);
        values.ShouldContain("Design APIs");
        values.ShouldContain("Code review");
        values.ShouldContain("Mentoring");
    }

    [Fact]
    public void Create_WithQualifications_ShouldAddAll()
    {
        var input = CreateValidInput();
        input.Qualifications = ["5+ years C#", "Cloud experience"];

        var job = Job.Create(input);

        job.Qualifications.Count().ShouldBe(2);
        var values = job.Qualifications.Select(q => q.Value);
        values.ShouldContain("5+ years C#");
        values.ShouldContain("Cloud experience");
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrowDomainException()
    {
        var input = CreateValidInput(title: "");

        var act = () => Job.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Title.Empty");
    }

    [Fact]
    public void Create_WithEmptyLocation_ShouldThrowDomainException()
    {
        var input = CreateValidInput(location: "");

        var act = () => Job.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Location.Empty");
    }

    [Fact]
    public void Create_WithInvalidCompanyId_ShouldThrowDomainException()
    {
        var input = CreateValidInput(companyId: 0);

        var act = () => Job.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Job.InvalidCompanyId");
    }

    [Fact]
    public void Create_WithInvalidJobType_ShouldThrowDomainException()
    {
        var input = CreateValidInput(jobType: (JobType)99);

        var act = () => Job.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Job.InvalidJobType");
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ShouldAccumulateErrors()
    {
        var input = CreateValidInput(title: "", location: "", aboutRole: "", companyId: 0);

        var act = () => Job.Create(input);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void AddResponsibility_WithNull_ShouldThrowDomainException()
    {
        var job = Job.Create(CreateValidInput());

        var act = () => job.AddResponsibility(null!);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldHaveSingleItem().Code.ShouldBe("Job.NullResponsibility");
    }

    [Fact]
    public void AddQualification_WithNull_ShouldThrowDomainException()
    {
        var job = Job.Create(CreateValidInput());

        var act = () => job.AddQualification(null!);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldHaveSingleItem().Code.ShouldBe("Job.NullQualification");
    }

    [Fact]
    public void RemoveResponsibility_WithNull_ShouldNotThrow()
    {
        var job = Job.Create(CreateValidInput());

        var act = () => job.RemoveResponsibility(null!);

        Should.NotThrow(act);
    }

    [Fact]
    public void RemoveQualification_WithNull_ShouldNotThrow()
    {
        var job = Job.Create(CreateValidInput());

        var act = () => job.RemoveQualification(null!);

        Should.NotThrow(act);
    }

    [Fact]
    public void SetTitle_WithValidValue_ShouldUpdateTitle()
    {
        var job = Job.Create(CreateValidInput());

        job.SetTitle("New Title");

        job.Title.ShouldBe("New Title");
    }

    [Fact]
    public void SetTitle_WithInvalidValue_ShouldThrowDomainException()
    {
        var job = Job.Create(CreateValidInput());

        var act = () => job.SetTitle("");

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void SetLocation_WithValidValue_ShouldUpdateLocation()
    {
        var job = Job.Create(CreateValidInput());

        job.SetLocation("London, UK");

        job.Location.ShouldBe("London, UK");
    }

    [Fact]
    public void SetJobType_ShouldUpdateJobType()
    {
        var job = Job.Create(CreateValidInput());

        job.SetJobType(JobType.Contract);

        job.JobType.ShouldBe(JobType.Contract);
    }

    [Fact]
    public void Create_WithAuditInfo_ShouldApplyAudit()
    {
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        var input = CreateValidInput();
        input.CreatedAt = createdAt;
        input.CreatedBy = "user@test.com";

        var job = Job.Create(input);

        job.CreatedAt.ShouldBe(createdAt);
        job.CreatedBy.ShouldBe("user@test.com");
        job.UpdatedAt.ShouldBe(createdAt);
        job.UpdatedBy.ShouldBe("user@test.com");
    }
}
