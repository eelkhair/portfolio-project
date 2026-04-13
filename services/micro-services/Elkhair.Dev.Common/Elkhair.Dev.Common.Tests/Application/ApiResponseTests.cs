using System.Net;
using Elkhair.Dev.Common.Application;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Application;

[Trait("Category", "Unit")]
public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var response = new ApiResponse<string>();

        // Assert
        response.Data.ShouldBeNull();
        response.Success.ShouldBeFalse();
        response.Exceptions.ShouldBeNull();
        response.StatusCode.ShouldBe(default(HttpStatusCode));
    }

    [Fact]
    public void ApiResponse_WithData_ShouldStoreData()
    {
        // Act
        var response = new ApiResponse<string>
        {
            Data = "test-data",
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        // Assert
        response.Data.ShouldBe("test-data");
        response.Success.ShouldBeTrue();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void ApiResponse_WithComplexType_ShouldStoreData()
    {
        // Arrange
        var data = new { Name = "Test", Value = 42 };

        // Act
        var response = new ApiResponse<object>
        {
            Data = data,
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        // Assert
        response.Data.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
    }

    [Fact]
    public void ApiResponse_WithError_ShouldStoreException()
    {
        // Arrange
        var error = new ApiError
        {
            Message = "Something went wrong",
            Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
            {
                { "Name", new[] { "Name is required" } }
            }
        };

        // Act
        var response = new ApiResponse<string>
        {
            Exceptions = error,
            Success = false,
            StatusCode = HttpStatusCode.BadRequest
        };

        // Assert
        response.Success.ShouldBeFalse();
        response.Exceptions.ShouldNotBeNull();
        response.Exceptions.Message.ShouldBe("Something went wrong");
        response.Exceptions.Errors.ShouldNotBeNull();
        response.Exceptions.Errors.ShouldContainKey("Name");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ApiResponse_WithListData_ShouldStoreList()
    {
        // Act
        var response = new ApiResponse<List<int>>
        {
            Data = new List<int> { 1, 2, 3 },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        // Assert
        response.Data.ShouldNotBeNull();
        response.Data.Count.ShouldBe(3);
    }

    [Fact]
    public void ApiResponse_WithNotFoundStatus_ShouldHaveCorrectCode()
    {
        // Act
        var response = new ApiResponse<string>
        {
            StatusCode = HttpStatusCode.NotFound,
            Success = false
        };

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Success.ShouldBeFalse();
    }

    [Fact]
    public void ApiError_DefaultValues_ShouldBeNull()
    {
        // Act
        var error = new ApiError();

        // Assert
        error.Message.ShouldBeNull();
        error.Errors.ShouldBeNull();
    }

    [Fact]
    public void ApiError_WithMultipleFieldErrors_ShouldStoreAll()
    {
        // Act
        var error = new ApiError
        {
            Message = "Validation failed",
            Errors = new Dictionary<string, string[]>
(StringComparer.Ordinal)
            {
                { "Name", new[] { "Name is required", "Name must be at least 3 characters" } },
                { "Email", new[] { "Email is invalid" } }
            }
        };

        // Assert
        error.Errors.ShouldNotBeNull();
        error.Errors.Count.ShouldBe(2);
        error.Errors["Name"].Length.ShouldBe(2);
        error.Errors["Email"].Length.ShouldBe(1);
    }
}
