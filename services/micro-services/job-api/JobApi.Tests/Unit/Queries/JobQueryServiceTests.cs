using JobApi.Application;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobApi.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class JobQueryServiceTests : IAsyncLifetime
{
    private JobDbContext _context = null!;
    private JobQueryService _sut = null!;
    private Company _company = null!;

    public async Task InitializeAsync()
    {
        MapsterSetup.Initialize();
        (_context, _company) = await TestDbContextFactory.CreateWithCompanyAsync();
        _sut = new JobQueryService(_context, Substitute.For<ILogger<JobQueryService>>());
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListAsync_ShouldReturnJobsForCompany()
    {
        var job1 = TestDbContextFactory.CreateJob(_company.Id, "Developer");
        var job2 = TestDbContextFactory.CreateJob(_company.Id, "Designer");
        _context.Jobs.AddRange(job1, job2);
        await _context.SaveChangesAsync();

        var result = await _sut.ListAsync(_company.UId, CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain(j => j.Title == "Developer");
        result.ShouldContain(j => j.Title == "Designer");
    }

    [Fact]
    public async Task ListAsync_ShouldNotReturnJobsFromOtherCompanies()
    {
        var otherCompany = TestDbContextFactory.CreateCompany("Other Corp");
        _context.Companies.Add(otherCompany);
        await _context.SaveChangesAsync();

        var myJob = TestDbContextFactory.CreateJob(_company.Id, "My Job");
        var otherJob = TestDbContextFactory.CreateJob(otherCompany.Id, "Other Job");
        _context.Jobs.AddRange(myJob, otherJob);
        await _context.SaveChangesAsync();

        var result = await _sut.ListAsync(_company.UId, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("My Job");
    }

    [Fact]
    public async Task ListAsync_ShouldReturnEmptyList_WhenNoJobs()
    {
        var result = await _sut.ListAsync(_company.UId, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_ShouldIncludeResponsibilitiesAndQualifications()
    {
        var job = TestDbContextFactory.CreateJob(_company.Id, "Full Stack");
        job.Responsibilities = new List<Responsibility>
        {
            new() { Value = "Build UIs", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "test", UpdatedBy = "test" },
            new() { Value = "Build APIs", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "test", UpdatedBy = "test" }
        };
        job.Qualifications = new List<Qualification>
        {
            new() { Value = "React", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "test", UpdatedBy = "test" }
        };
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var result = await _sut.ListAsync(_company.UId, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Responsibilities.Count.ShouldBe(2);
        result[0].Qualifications.Count.ShouldBe(1);
        result[0].Responsibilities.ShouldContain("Build UIs");
        result[0].Qualifications.ShouldContain("React");
    }

    [Fact]
    public async Task ListCompanyJobSummariesAsync_ShouldReturnAllCompaniesWithJobs()
    {
        var company2 = TestDbContextFactory.CreateCompany("Corp B");
        _context.Companies.Add(company2);
        await _context.SaveChangesAsync();

        _context.Jobs.Add(TestDbContextFactory.CreateJob(_company.Id, "Job A1"));
        _context.Jobs.Add(TestDbContextFactory.CreateJob(_company.Id, "Job A2"));
        _context.Jobs.Add(TestDbContextFactory.CreateJob(company2.Id, "Job B1"));
        await _context.SaveChangesAsync();

        var result = await _sut.ListCompanyJobSummariesAsync(CancellationToken.None);

        result.Count.ShouldBe(2);
        var companyA = result.First(r => r.CompanyName == "Test Corp");
        companyA.JobCount.ShouldBe(2);
        companyA.Jobs.Count.ShouldBe(2);

        var companyB = result.First(r => r.CompanyName == "Corp B");
        companyB.JobCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListCompanyJobSummariesAsync_ShouldReturnEmptyJobs_ForCompanyWithNoJobs()
    {
        var result = await _sut.ListCompanyJobSummariesAsync(CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].CompanyName.ShouldBe("Test Corp");
        result[0].JobCount.ShouldBe(0);
        result[0].Jobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListCompanyJobSummariesAsync_ShouldMapJobSummaryFields()
    {
        var job = TestDbContextFactory.CreateJob(_company.Id, "Senior Dev");
        job.Location = "Berlin";
        job.SalaryRange = "$120k";
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var result = await _sut.ListCompanyJobSummariesAsync(CancellationToken.None);

        var summary = result[0].Jobs[0];
        summary.Title.ShouldBe("Senior Dev");
        summary.Location.ShouldBe("Berlin");
        summary.SalaryRange.ShouldBe("$120k");
    }
}
