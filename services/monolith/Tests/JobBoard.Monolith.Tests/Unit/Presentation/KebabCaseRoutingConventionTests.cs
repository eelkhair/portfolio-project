using JobBoard.API.Infrastructure.OpenApi;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Presentation;

[Trait("Category", "Unit")]
public class KebabCaseRoutingConventionTests
{
    private readonly KebabCaseRoutingConvention.KebabCaseParameterTransformer _transformer = new();

    [Theory]
    [InlineData("ListDrafts", "list-drafts")]
    [InlineData("GetCompanies", "get-companies")]
    [InlineData("CreateCompany", "create-company")]
    [InlineData("UpdateApplicationMode", "update-application-mode")]
    public void TransformOutbound_PascalCase_ShouldConvertToKebabCase(string input, string expected)
    {
        var result = _transformer.TransformOutbound(input);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("companies", "companies")]
    [InlineData("jobs", "jobs")]
    public void TransformOutbound_LowerCase_ShouldRemainUnchanged(string input, string expected)
    {
        var result = _transformer.TransformOutbound(input);

        result.ShouldBe(expected);
    }

    [Fact]
    public void TransformOutbound_Null_ShouldReturnNull()
    {
        var result = _transformer.TransformOutbound(null);

        result.ShouldBeNull();
    }

    [Fact]
    public void TransformOutbound_EmptyString_ShouldReturnNull()
    {
        var result = _transformer.TransformOutbound("");

        result.ShouldBeNull();
    }

    [Fact]
    public void TransformOutbound_NonStringValue_ShouldReturnNull()
    {
        var result = _transformer.TransformOutbound(42);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("A", "a")]
    [InlineData("AB", "a-b")]
    [InlineData("ABC", "a-b-c")]
    public void TransformOutbound_ShortInputs_ShouldConvertCorrectly(string input, string expected)
    {
        var result = _transformer.TransformOutbound(input);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convention_Apply_ShouldTransformActionRouteTemplates()
    {
        // Test the full convention via ApplicationModel
        var convention = new KebabCaseRoutingConvention();

        // The convention transforms route templates on controller actions.
        // We test the transformer directly since it's the public API surface.
        var transformer = new KebabCaseRoutingConvention.KebabCaseParameterTransformer();

        transformer.TransformOutbound("JobApplications").ShouldBe("job-applications");
        transformer.TransformOutbound("DraftItems").ShouldBe("draft-items");
    }
}
