using System.Security.Claims;
using Elkhair.Dev.Common.Dapr;
using JobApi.Application;
using JobApi.Infrastructure.Data;
using JobApi.Tests.Helpers;
using JobAPI.Contracts.Enums;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobBoard.IntegrationEvents.Job;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace JobApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class JobCommandServiceTests : IAsyncLifetime
{
    private readonly IMessageSender _messageSender = Substitute.For<IMessageSender>();
    private JobDbContext _context = null!;
    private JobCommandService _sut = null!;
    private Infrastructure.Data.Entities.Company _company = null!;

    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
    [
        new Claim("sub", "user-123")
    ]));

    public async Task InitializeAsync()
    {
        MapsterSetup.Initialize();
        (_context, _company) = await TestDbContextFactory.CreateWithCompanyAsync();
        _sut = new JobCommandService(_context, _messageSender);
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateJobAsync_ShouldPersistJob()
    {
        var request = new CreateJobRequest
        {
            Title = "Backend Dev",
            CompanyUId = _company.UId,
            Location = "Remote",
            JobType = JobType.FullTime,
            AboutRole = "Build APIs",
            Responsibilities = ["Design APIs", "Write tests"],
            Qualifications = ["C#", ".NET"]
        };

        await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: false);

        var saved = await _context.Jobs.Include(j => j.Responsibilities).Include(j => j.Qualifications).FirstAsync();
        saved.Title.ShouldBe("Backend Dev");
        saved.Location.ShouldBe("Remote");
        saved.AboutRole.ShouldBe("Build APIs");
        saved.CompanyId.ShouldBe(_company.Id);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldSetCompanyIdFromLookup()
    {
        var request = new CreateJobRequest
        {
            Title = "Frontend Dev",
            CompanyUId = _company.UId,
            Location = "NYC",
            JobType = JobType.Contract,
            AboutRole = "Build UIs"
        };

        await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: false);

        var saved = await _context.Jobs.FirstAsync();
        saved.CompanyId.ShouldBe(_company.Id);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldMapResponsibilitiesAndQualifications()
    {
        var request = new CreateJobRequest
        {
            Title = "DevOps",
            CompanyUId = _company.UId,
            Location = "London",
            JobType = JobType.FullTime,
            AboutRole = "Manage infra",
            Responsibilities = ["CI/CD", "Monitoring"],
            Qualifications = ["Kubernetes", "Terraform"]
        };

        var result = await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: false);

        var saved = await _context.Jobs.Include(j => j.Responsibilities).Include(j => j.Qualifications).FirstAsync();
        saved.Responsibilities.Count.ShouldBe(2);
        saved.Qualifications.Count.ShouldBe(2);
        saved.Responsibilities.Select(r => r.Value).ShouldContain("CI/CD");
        saved.Qualifications.Select(q => q.Value).ShouldContain("Kubernetes");
    }

    [Fact]
    public async Task CreateJobAsync_ShouldReturnMappedJobResponse()
    {
        var request = new CreateJobRequest
        {
            Title = "Data Engineer",
            CompanyUId = _company.UId,
            Location = "Berlin",
            JobType = JobType.PartTime,
            AboutRole = "ETL pipelines",
            SalaryRange = "$80k-$120k"
        };

        var result = await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: false);

        result.Title.ShouldBe("Data Engineer");
        result.Location.ShouldBe("Berlin");
        result.SalaryRange.ShouldBe("$80k-$120k");
    }

    [Fact]
    public async Task CreateJobAsync_WhenPublishEventTrue_ShouldPublishMicroJobCreatedV1Event()
    {
        var request = new CreateJobRequest
        {
            Title = "ML Engineer",
            CompanyUId = _company.UId,
            Location = "SF",
            JobType = JobType.FullTime,
            AboutRole = "Train models",
            Responsibilities = ["Research"],
            Qualifications = ["Python"]
        };

        await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: true);

        await _messageSender.Received(1).SendEventAsync(
            "rabbitmq.pubsub", "micro.job-created.v1", "user-123",
            Arg.Is<MicroJobCreatedV1Event>(e =>
                e.Title == "ML Engineer" &&
                e.CompanyUId == _company.UId &&
                e.Location == "SF" &&
                e.UserId == "user-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateJobAsync_WhenPublishEventFalse_ShouldNotPublishEvent()
    {
        var request = new CreateJobRequest
        {
            Title = "QA Engineer",
            CompanyUId = _company.UId,
            Location = "Austin",
            JobType = JobType.Internship,
            AboutRole = "Test software"
        };

        await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: false);

        await _messageSender.DidNotReceive().SendEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MicroJobCreatedV1Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateJobAsync_ShouldSetSalaryRangeToNull_WhenNotProvided()
    {
        var request = new CreateJobRequest
        {
            Title = "Intern",
            CompanyUId = _company.UId,
            Location = "Remote",
            JobType = JobType.Internship,
            AboutRole = "Learn and grow"
        };

        await _sut.CreateJobAsync(request, _user, CancellationToken.None, publishEvent: false);

        var saved = await _context.Jobs.FirstAsync();
        saved.SalaryRange.ShouldBeNull();
    }
}
