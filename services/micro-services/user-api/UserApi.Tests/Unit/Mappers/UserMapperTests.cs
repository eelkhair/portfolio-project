using Shouldly;
using UserApi.Features.Users.Mappers;
using UserApi.Infrastructure.Data.Entities;
using UserAPI.Contracts.Models.Responses;

namespace UserApi.Tests.Unit.Mappers;

[Trait("Category", "Unit")]
public class UserMapperTests
{
    private readonly UserMapper _mapper = new();

    // ── FromEntity ──

    [Fact]
    public void FromEntity_ShouldMapEmail()
    {
        var user = CreateUser();

        var response = _mapper.FromEntity(user);

        response.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void FromEntity_ShouldMapUId()
    {
        var uid = Guid.NewGuid();
        var user = CreateUser(uid: uid);

        var response = _mapper.FromEntity(user);

        response.UId.ShouldBe(uid);
    }

    [Fact]
    public void FromEntity_ShouldMapCreatedAt()
    {
        var createdAt = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var user = CreateUser(createdAt: createdAt);

        var response = _mapper.FromEntity(user);

        response.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void FromEntity_ShouldMapUpdatedAt()
    {
        var updatedAt = new DateTime(2024, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var user = CreateUser(updatedAt: updatedAt);

        var response = _mapper.FromEntity(user);

        response.UpdatedAt.ShouldBe(updatedAt);
    }

    [Fact]
    public void FromEntity_ShouldHandleNullUpdatedAt()
    {
        var user = CreateUser(updatedAt: null);

        var response = _mapper.FromEntity(user);

        response.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void FromEntity_ShouldMapAllFieldsCorrectly()
    {
        var uid = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = CreateUser(uid: uid, email: "full@test.com", createdAt: createdAt, updatedAt: updatedAt);

        var response = _mapper.FromEntity(user);

        response.UId.ShouldBe(uid);
        response.Email.ShouldBe("full@test.com");
        response.CreatedAt.ShouldBe(createdAt);
        response.UpdatedAt.ShouldBe(updatedAt);
    }

    [Fact]
    public void FromEntity_ShouldReturnUserResponseType()
    {
        var user = CreateUser();

        var response = _mapper.FromEntity(user);

        response.ShouldBeOfType<UserResponse>();
    }

    [Fact]
    public void FromEntity_ShouldNotMapFirstNameOrLastName()
    {
        // UserResponse doesn't have FirstName/LastName — verify the mapper
        // only maps the fields present in UserResponse
        var user = CreateUser();
        user.FirstName = "John";
        user.LastName = "Doe";

        var response = _mapper.FromEntity(user);

        // Response should still be valid (no FirstName/LastName on UserResponse)
        response.Email.ShouldBe("test@example.com");
    }

    private static User CreateUser(
        Guid? uid = null,
        string email = "test@example.com",
        DateTime? createdAt = null,
        DateTime? updatedAt = null) => new()
    {
        UId = uid ?? Guid.NewGuid(),
        Email = email,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = updatedAt,
        FirstName = "Test",
        LastName = "User",
        UserCompanies = new List<UserCompany>()
    };
}
