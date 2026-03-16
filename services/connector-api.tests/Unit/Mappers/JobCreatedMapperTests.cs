using ConnectorAPI.Mappers;
using ConnectorAPI.Models.JobCreated;
using JobBoard.IntegrationEvents.Job;
using Shouldly;

namespace connector_api.tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class JobCreatedMapperTests
{
    [Fact]
    public void Map_AllFields_MapsCorrectly()
    {
        var uid = Guid.NewGuid();
        var companyUId = Guid.NewGuid();
        var evt = new JobCreatedV1Event(
            UId: uid,
            CompanyUId: companyUId,
            Title: "Full Stack Developer",
            AboutRole: "Build full stack apps",
            Location: "Austin, TX",
            SalaryRange: "$110k-$140k",
            DraftId: Guid.NewGuid().ToString(),
            DeleteDraft: false,
            Responsibilities: ["Frontend", "Backend"],
            Qualifications: ["React", "Node.js"],
            JobType: "FullTime")
        {
            UserId = "user-job-1"
        };

        var result = JobCreatedMapper.Map(evt);

        result.UId.ShouldBe(uid);
        result.Title.ShouldBe("Full Stack Developer");
        result.CompanyUId.ShouldBe(companyUId);
        result.Location.ShouldBe("Austin, TX");
        result.JobType.ShouldBe(JobType.FullTime);
        result.AboutRole.ShouldBe("Build full stack apps");
        result.SalaryRange.ShouldBe("$110k-$140k");
        result.Responsibilities.ShouldBe(new List<string> { "Frontend", "Backend" });
        result.Qualifications.ShouldBe(new List<string> { "React", "Node.js" });
    }

    [Fact]
    public void Map_CaseInsensitiveJobType()
    {
        var evt = new JobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Intern",
            AboutRole: "Learn",
            Location: "Remote",
            SalaryRange: null,
            DraftId: null,
            DeleteDraft: false,
            Responsibilities: [],
            Qualifications: [],
            JobType: "fulltime")
        {
            UserId = "user-job-2"
        };

        var result = JobCreatedMapper.Map(evt);

        result.JobType.ShouldBe(JobType.FullTime);
    }
}
