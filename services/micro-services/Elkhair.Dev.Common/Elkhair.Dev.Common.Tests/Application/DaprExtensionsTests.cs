using System.Security.Claims;
using Elkhair.Dev.Common.Application;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Application;

[Trait("Category", "Unit")]
public class DaprExtensionsTests
{
    [Fact]
    public void CreateUser_WithUserId_ReturnsClaimsPrincipalWithSubClaim()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var principal = DaprExtensions.CreateUser(userId);

        // Assert
        principal.ShouldNotBeNull();
        principal.Identity.ShouldNotBeNull();
        principal.Identity!.IsAuthenticated.ShouldBeTrue();
        principal.FindFirst("sub")!.Value.ShouldBe(userId);
    }

    [Fact]
    public void CreateUser_WithUserId_HasTestAuthType()
    {
        // Arrange
        var userId = "test-user-456";

        // Act
        var principal = DaprExtensions.CreateUser(userId);

        // Assert
        principal.Identity!.AuthenticationType.ShouldBe("TestAuthType");
    }

    [Fact]
    public void CreateUser_WithEmptyUserId_ReturnsClaimsPrincipalWithEmptySub()
    {
        // Act
        var principal = DaprExtensions.CreateUser(string.Empty);

        // Assert
        principal.FindFirst("sub")!.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void CreateUser_WithGuidUserId_ReturnsCorrectClaim()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var principal = DaprExtensions.CreateUser(userId);

        // Assert
        principal.GetUserId().ShouldBe(userId);
    }

    [Fact]
    public async Task Process_WithSuccessfulFunction_ReturnsSuccessResponse()
    {
        // Act
        var result = await DaprExtensions.Process(async () =>
        {
            await Task.CompletedTask;
            return "success-data";
        });

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Data.ShouldBe("success-data");
        result.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Process_WithSuccessfulFunction_ReturnsComplexType()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };

        // Act
        var result = await DaprExtensions.Process(async () =>
        {
            await Task.CompletedTask;
            return data;
        });

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Process_WithNullReturnValue_ReturnsSuccessWithNullData()
    {
        // Act
        var result = await DaprExtensions.Process<string?>(async () =>
        {
            await Task.CompletedTask;
            return null;
        });

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldBeNull();
        result.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }
}
