using Shouldly;
using JobBoard.Domain.Entities.Users;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Monolith.Tests.Unit.Domain.Users;

[Trait("Category", "Unit")]
public class UserTests
{
    [Fact]
    public void Create_WithValidInput_ShouldReturnUser()
    {
        var id = Guid.NewGuid();

        var user = User.Create("John", "Doe", "john@example.com", "auth0|123", id, 1);

        user.FirstName.ShouldBe("John");
        user.LastName.ShouldBe("Doe");
        user.Email.ShouldBe("john@example.com");
        user.ExternalId.ShouldBe("auth0|123");
        user.Id.ShouldBe(id);
        user.InternalId.ShouldBe(1);
    }

    [Fact]
    public void Create_WithNullExternalId_ShouldSucceed()
    {
        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        user.ExternalId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithAuditInfo_ShouldApplyAudit()
    {
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1,
            createdAt, "admin@test.com");

        user.CreatedAt.ShouldBe(createdAt);
        user.CreatedBy.ShouldBe("admin@test.com");
        user.UpdatedAt.ShouldBe(createdAt);
        user.UpdatedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void Create_WithEmptyFirstName_ShouldThrowDomainException()
    {
        var act = () => User.Create("", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "FirstName.Empty");
    }

    [Fact]
    public void Create_WithEmptyLastName_ShouldThrowDomainException()
    {
        var act = () => User.Create("John", "", "john@example.com", null, Guid.NewGuid(), 1);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "LastName.Empty");
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrowDomainException()
    {
        var act = () => User.Create("John", "Doe", "not-valid", null, Guid.NewGuid(), 1);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.ShouldContain(e => e.Code == "Email.InvalidFormat");
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ShouldAccumulateErrors()
    {
        var act = () => User.Create("", "", "", null, Guid.NewGuid(), 1);

        var ex = Should.Throw<DomainException>(act);
        ex.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void SetFirstName_WithValidValue_ShouldUpdate()
    {
        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        user.SetFirstName("Jane");

        user.FirstName.ShouldBe("Jane");
    }

    [Fact]
    public void SetFirstName_WithInvalidValue_ShouldThrowDomainException()
    {
        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        var act = () => user.SetFirstName("");

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void SetLastName_WithValidValue_ShouldUpdate()
    {
        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        user.SetLastName("Smith");

        user.LastName.ShouldBe("Smith");
    }

    [Fact]
    public void SetEmail_WithValidValue_ShouldUpdate()
    {
        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        user.SetEmail("jane@example.com");

        user.Email.ShouldBe("jane@example.com");
    }

    [Fact]
    public void SetExternalId_WithValidValue_ShouldUpdate()
    {
        var user = User.Create("John", "Doe", "john@example.com", null, Guid.NewGuid(), 1);

        user.SetExternalId("auth0|456");

        user.ExternalId.ShouldBe("auth0|456");
    }
}
