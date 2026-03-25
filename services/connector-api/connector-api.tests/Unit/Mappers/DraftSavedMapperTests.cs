using ConnectorAPI.Mappers;
using JobBoard.IntegrationEvents.Draft;
using Shouldly;

namespace connector_api.tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class DraftSavedMapperTests
{
    [Fact]
    public void Map_AllFields_MapsCorrectly()
    {
        var uid = Guid.NewGuid();
        var evt = new DraftSavedV1Event(
            UId: uid,
            CompanyUId: Guid.NewGuid(),
            Title: "Backend Developer",
            AboutRole: "Build APIs",
            Location: "Berlin",
            JobType: "FullTime",
            SalaryRange: "$90k-$120k",
            Notes: "Urgent hire",
            Responsibilities: ["Design APIs", "Write tests"],
            Qualifications: ["C#", "SQL"])
        {
            UserId = "user-map-1"
        };

        var result = DraftSavedMapper.Map(evt);

        result.Id.ShouldBe(uid.ToString());
        result.Title.ShouldBe("Backend Developer");
        result.AboutRole.ShouldBe("Build APIs");
        result.Location.ShouldBe("Berlin");
        result.JobType.ShouldBe("FullTime");
        result.SalaryRange.ShouldBe("$90k-$120k");
        result.Notes.ShouldBe("Urgent hire");
        result.Responsibilities.ShouldBe(new List<string> { "Design APIs", "Write tests" });
        result.Qualifications.ShouldBe(new List<string> { "C#", "SQL" });
    }

    [Fact]
    public void Map_NullSalaryRange_DefaultsToEmptyString()
    {
        var evt = new DraftSavedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Role",
            AboutRole: "About",
            Location: "Remote",
            JobType: "Contract",
            SalaryRange: null,
            Notes: "",
            Responsibilities: [],
            Qualifications: [])
        {
            UserId = "user-map-2"
        };

        var result = DraftSavedMapper.Map(evt);

        result.SalaryRange.ShouldBe("");
    }
}
