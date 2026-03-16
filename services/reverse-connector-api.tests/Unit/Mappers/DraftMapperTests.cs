using System.Text.Json;
using JobBoard.IntegrationEvents.Draft;
using ReverseConnectorAPI.Mappers;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Mappers;

public class DraftMapperTests
{
    [Fact]
    public void ToPayload_MapsBasicFields()
    {
        var draftId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var evt = new DraftSavedV1Event(
            UId: draftId,
            CompanyUId: companyId,
            Title: "Senior Engineer",
            AboutRole: "Build stuff",
            Location: "Remote",
            JobType: "FullTime",
            SalaryRange: "$100k-$150k",
            Notes: "Some notes",
            Responsibilities: ["Design systems", "Code reviews"],
            Qualifications: ["5+ years C#", "Distributed systems"]
        );

        var result = DraftMapper.ToPayload(evt);

        result.DraftId.ShouldBe(draftId);
        result.CompanyId.ShouldBe(companyId);
    }

    [Fact]
    public void ToPayload_ContentJson_ContainsAllFields()
    {
        var draftId = Guid.NewGuid();
        var evt = new DraftSavedV1Event(
            UId: draftId,
            CompanyUId: Guid.NewGuid(),
            Title: "Backend Dev",
            AboutRole: "API development",
            Location: "NYC",
            JobType: "Contract",
            SalaryRange: null,
            Notes: "",
            Responsibilities: ["Build APIs"],
            Qualifications: ["3+ years"]
        );

        var result = DraftMapper.ToPayload(evt);

        var json = JsonDocument.Parse(result.ContentJson);
        var root = json.RootElement;
        root.GetProperty("id").GetString().ShouldBe(draftId.ToString());
        root.GetProperty("title").GetString().ShouldBe("Backend Dev");
        root.GetProperty("aboutRole").GetString().ShouldBe("API development");
        root.GetProperty("location").GetString().ShouldBe("NYC");
        root.GetProperty("jobType").GetString().ShouldBe("Contract");
        root.GetProperty("responsibilities").GetArrayLength().ShouldBe(1);
        root.GetProperty("qualifications").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public void ToPayload_WithNullSalaryRange_SerializesAsNull()
    {
        var evt = new DraftSavedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Test",
            AboutRole: "Test",
            Location: "Test",
            JobType: "FullTime",
            SalaryRange: null,
            Notes: "",
            Responsibilities: [],
            Qualifications: []
        );

        var result = DraftMapper.ToPayload(evt);

        var json = JsonDocument.Parse(result.ContentJson);
        json.RootElement.GetProperty("salaryRange").ValueKind.ShouldBe(JsonValueKind.Null);
    }
}
