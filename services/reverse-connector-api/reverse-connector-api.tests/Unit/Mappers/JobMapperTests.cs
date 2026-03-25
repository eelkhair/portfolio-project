using JobBoard.IntegrationEvents.Job;
using ReverseConnectorAPI.Mappers;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Mappers;

public class JobMapperTests
{
    [Fact]
    public void ToPayload_MapsAllFields()
    {
        var jobId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var evt = new MicroJobCreatedV1Event(
            UId: jobId,
            CompanyUId: companyId,
            Title: "Senior Backend Engineer",
            AboutRole: "Build distributed systems",
            Location: "Remote",
            SalaryRange: "$150k-$200k",
            JobType: "FullTime",
            Responsibilities: ["Design APIs", "Mentor juniors", "Code reviews"],
            Qualifications: ["7+ years experience", "C# expertise", "Cloud platforms"]
        );

        var result = JobMapper.ToPayload(evt);

        result.JobId.ShouldBe(jobId);
        result.CompanyId.ShouldBe(companyId);
        result.Title.ShouldBe("Senior Backend Engineer");
        result.AboutRole.ShouldBe("Build distributed systems");
        result.Location.ShouldBe("Remote");
        result.SalaryRange.ShouldBe("$150k-$200k");
        result.JobType.ShouldBe("FullTime");
        result.Responsibilities.ShouldBe(["Design APIs", "Mentor juniors", "Code reviews"]);
        result.Qualifications.ShouldBe(["7+ years experience", "C# expertise", "Cloud platforms"]);
    }

    [Fact]
    public void ToPayload_WithNullSalaryRange_MapsNull()
    {
        var evt = new MicroJobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Intern",
            AboutRole: "Learn",
            Location: "On-site",
            SalaryRange: null,
            JobType: "Internship",
            Responsibilities: [],
            Qualifications: []
        );

        var result = JobMapper.ToPayload(evt);

        result.SalaryRange.ShouldBeNull();
        result.Responsibilities.ShouldBeEmpty();
        result.Qualifications.ShouldBeEmpty();
    }

    [Fact]
    public void ToPayload_WithMultipleListItems_PreservesOrder()
    {
        var evt = new MicroJobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Order Test",
            AboutRole: "Test",
            Location: "Remote",
            SalaryRange: "$100k",
            JobType: "FullTime",
            Responsibilities: ["First", "Second", "Third"],
            Qualifications: ["Alpha", "Beta"]
        );

        var result = JobMapper.ToPayload(evt);

        result.Responsibilities[0].ShouldBe("First");
        result.Responsibilities[1].ShouldBe("Second");
        result.Responsibilities[2].ShouldBe("Third");
        result.Qualifications[0].ShouldBe("Alpha");
        result.Qualifications[1].ShouldBe("Beta");
    }

    [Fact]
    public void ToPayload_WithVariousJobTypes_PreservesJobType()
    {
        var evt = new MicroJobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Type Test",
            AboutRole: "Test",
            Location: "Remote",
            SalaryRange: null,
            JobType: "Contract",
            Responsibilities: [],
            Qualifications: []
        );

        var result = JobMapper.ToPayload(evt);

        result.JobType.ShouldBe("Contract");
    }
}
