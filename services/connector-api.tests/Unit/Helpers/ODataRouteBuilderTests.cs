using ConnectorAPI.Helpers;
using Shouldly;

namespace connector_api.tests.Unit.Helpers;

[Trait("Category", "Unit")]
public class ODataRouteBuilderTests
{
    private readonly Guid _testId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void CompanyById_NoQuery_ShouldReturnBasePath()
    {
        var result = ODataRouteBuilder.CompanyById(_testId);

        result.ShouldBe($"odata/companies({_testId})");
    }

    [Fact]
    public void CompanyById_WithSelect_ShouldAppendQueryString()
    {
        var result = ODataRouteBuilder.CompanyById(_testId, q =>
        {
            q["$select"] = "Name,Email";
        });

        result.ShouldContain("%24select=Name%2cEmail");
        result.ShouldStartWith($"odata/companies({_testId})?");
    }

    [Fact]
    public void CompanyById_WithMultipleParams_ShouldAppendAll()
    {
        var result = ODataRouteBuilder.CompanyById(_testId, q =>
        {
            q["$select"] = "Name";
            q["$filter"] = "IsActive eq true";
        });

        result.ShouldContain("%24select=");
        result.ShouldContain("%24filter=");
    }

    [Fact]
    public void UserById_NoQuery_ShouldReturnBasePath()
    {
        var result = ODataRouteBuilder.UserById(_testId);

        result.ShouldBe($"odata/users({_testId})");
    }

    [Fact]
    public void UserById_WithSelect_ShouldAppendQueryString()
    {
        var result = ODataRouteBuilder.UserById(_testId, q =>
        {
            q["$select"] = "FirstName,LastName";
        });

        result.ShouldContain("%24select=");
        result.ShouldStartWith($"odata/users({_testId})?");
    }

    [Fact]
    public void CompanyById_NullConfigureQuery_ShouldReturnBasePath()
    {
        var result = ODataRouteBuilder.CompanyById(_testId, null);

        result.ShouldNotContain("?");
        result.ShouldBe($"odata/companies({_testId})");
    }

    [Fact]
    public void CompanyById_EmptyConfigureQuery_ShouldReturnBasePath()
    {
        var result = ODataRouteBuilder.CompanyById(_testId, _ => { });

        result.ShouldNotContain("?");
        result.ShouldBe($"odata/companies({_testId})");
    }
}
