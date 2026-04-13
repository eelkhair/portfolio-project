using System.Net;
using System.Text.Json;
using Shouldly;
using UserApi.Infrastructure.Keycloak;
using UserApi.Tests.Helpers;

namespace UserApi.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class KeycloakResourceTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly KeycloakResource _sut;
    private const string BaseUrl = "https://auth.test.com/admin/realms/test-realm";

    public KeycloakResourceTests()
    {
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri(BaseUrl) };
        _sut = new KeycloakResource(httpClient, BaseUrl);
    }

    #region CreateGroupAsync

    [Fact]
    public async Task CreateGroupAsync_ShouldFindCompaniesParentAndCreateSubGroup()
    {
        // Arrange
        var companiesGroupId = Guid.NewGuid().ToString();
        var newGroupId = Guid.NewGuid().ToString();
        var companyUId = Guid.NewGuid();

        // 1st call: GET /groups?search=Companies&exact=true
        _handler.EnqueueResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new[] { new { id = companiesGroupId, name = "Companies" } }));

        // 2nd call: POST /groups/{parentId}/children  -> 201 with Location header
        _handler.EnqueueResponse(HttpStatusCode.Created, "{}",
            new Uri($"{BaseUrl}/groups/{newGroupId}"));

        // Act
        var result = await _sut.CreateGroupAsync(companyUId, "Acme Inc", CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Id.ShouldBe(newGroupId);

        _handler.AllRequests.Count.ShouldBe(2);
        _handler.AllRequests[0].Method.ShouldBe(HttpMethod.Get);
        _handler.AllRequests[0].RequestUri!.ToString().ShouldContain("groups?search=Companies");
        _handler.AllRequests[1].Method.ShouldBe(HttpMethod.Post);
        _handler.AllRequests[1].RequestUri!.ToString().ShouldContain($"groups/{companiesGroupId}/children");
    }

    [Fact]
    public async Task CreateGroupAsync_WhenCompaniesGroupNotFound_ShouldReturnFailure()
    {
        // Arrange - return empty array for group search
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        // Act
        var result = await _sut.CreateGroupAsync(Guid.NewGuid(), "Test", CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Message.ShouldContain("Companies");
    }

    #endregion

    #region CreateSubGroupAsync

    [Fact]
    public async Task CreateSubGroupAsync_Success_ShouldReturnCreatedGroup()
    {
        var parentId = Guid.NewGuid().ToString();
        var newId = Guid.NewGuid().ToString();

        _handler.EnqueueResponse(HttpStatusCode.Created, "{}",
            new Uri($"{BaseUrl}/groups/{newId}"));

        var result = await _sut.CreateSubGroupAsync(parentId, "CompanyAdmins", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.Data!.Id.ShouldBe(newId);
        result.Data.Name.ShouldBe("CompanyAdmins");
    }

    [Fact]
    public async Task CreateSubGroupAsync_Conflict409_ShouldFindExistingGroup()
    {
        var parentId = Guid.NewGuid().ToString();
        var existingId = Guid.NewGuid().ToString();

        // 1st: POST returns 409
        _handler.EnqueueResponse(HttpStatusCode.Conflict);

        // 2nd: GET children returns existing group
        _handler.EnqueueResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new[]
            {
                new { id = existingId, name = "CompanyAdmins" }
            }));

        var result = await _sut.CreateSubGroupAsync(parentId, "CompanyAdmins", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Data!.Id.ShouldBe(existingId);
        result.Data.Name.ShouldBe("CompanyAdmins");
    }

    [Fact]
    public async Task CreateSubGroupAsync_Conflict409_WhenGroupNotFoundInChildren_ShouldReturnFailure()
    {
        var parentId = Guid.NewGuid().ToString();

        // 1st: POST returns 409
        _handler.EnqueueResponse(HttpStatusCode.Conflict);

        // 2nd: GET children returns empty (group not actually there)
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        var result = await _sut.CreateSubGroupAsync(parentId, "CompanyAdmins", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
    }

    #endregion

    #region CreateUserAsync

    [Fact]
    public async Task CreateUserAsync_NewUser_ShouldPostAndReturnCreated()
    {
        var newUserId = Guid.NewGuid().ToString();

        // 1st: GET /users?email=...  -> empty (user doesn't exist)
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        // 2nd: POST /users  -> 201 with Location header
        _handler.EnqueueResponse(HttpStatusCode.Created, "{}",
            new Uri($"{BaseUrl}/users/{newUserId}"));

        var attributes = new Dictionary<string, List<string>>
(StringComparer.Ordinal)
        {
            ["companyName"] = ["Acme Inc"]
        };

        var result = await _sut.CreateUserAsync("jane@test.com", "Jane", "Doe", attributes, CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.Data!.Id.ShouldBe(newUserId);
        result.Data.Email.ShouldBe("jane@test.com");

        // Verify the POST body contains user fields
        _handler.AllRequests[1].Method.ShouldBe(HttpMethod.Post);
        _handler.AllRequestBodies[1].ShouldContain("jane@test.com");
        _handler.AllRequestBodies[1].ShouldContain("Jane");
        _handler.AllRequestBodies[1].ShouldContain("Doe");
        _handler.AllRequestBodies[1].ShouldContain("companyName");
    }

    [Fact]
    public async Task CreateUserAsync_ExistingUser_ShouldReturnOkAndUpdateAttributes()
    {
        var existingId = Guid.NewGuid().ToString();

        // 1st: GET /users?email=... -> existing user
        _handler.EnqueueResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new[]
            {
                new { id = existingId, email = "jane@test.com", firstName = "Jane", lastName = "Doe" }
            }));

        // 2nd: PUT /users/{id} -> update attributes
        _handler.EnqueueResponse(HttpStatusCode.NoContent);

        var attributes = new Dictionary<string, List<string>>
(StringComparer.Ordinal)
        {
            ["companyName"] = ["New Corp"]
        };

        var result = await _sut.CreateUserAsync("jane@test.com", "Jane", "Doe", attributes, CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Data!.Id.ShouldBe(existingId);

        // Should have called PUT to update attributes
        _handler.AllRequests[1].Method.ShouldBe(HttpMethod.Put);
        _handler.AllRequests[1].RequestUri!.ToString().ShouldContain($"users/{existingId}");
    }

    [Fact]
    public async Task CreateUserAsync_ExistingUserNoAttributes_ShouldNotCallPut()
    {
        var existingId = Guid.NewGuid().ToString();

        // GET /users?email=... -> existing user
        _handler.EnqueueResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new[]
            {
                new { id = existingId, email = "jane@test.com" }
            }));

        var result = await _sut.CreateUserAsync("jane@test.com", "Jane", "Doe", null, CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Should NOT have made a second PUT call
        _handler.AllRequests.Count.ShouldBe(1);
    }

    #endregion

    #region AddUserToGroupAsync

    [Fact]
    public async Task AddUserToGroupAsync_ShouldSendPutToCorrectUrl()
    {
        var userId = Guid.NewGuid().ToString();
        var groupId = Guid.NewGuid().ToString();

        _handler.SetResponse(HttpStatusCode.NoContent);

        var result = await _sut.AddUserToGroupAsync(userId, groupId, CancellationToken.None);

        result.Success.ShouldBeTrue();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        _handler.LastRequest.RequestUri!.ToString()
            .ShouldBe($"{BaseUrl}/users/{userId}/groups/{groupId}");
    }

    [Fact]
    public async Task AddUserToGroupAsync_WhenFails_ShouldReturnFailure()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var result = await _sut.AddUserToGroupAsync("u1", "g1", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Exceptions.ShouldNotBeNull();
    }

    #endregion

    #region SendVerifyEmailAsync

    [Fact]
    public async Task SendVerifyEmailAsync_ShouldSendPutToCorrectUrl()
    {
        var userId = Guid.NewGuid().ToString();

        _handler.SetResponse(HttpStatusCode.NoContent);

        var result = await _sut.SendVerifyEmailAsync(userId, CancellationToken.None);

        result.Success.ShouldBeTrue();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        _handler.LastRequest.RequestUri!.ToString()
            .ShouldBe($"{BaseUrl}/users/{userId}/send-verify-email");
    }

    [Fact]
    public async Task SendVerifyEmailAsync_WhenFails_ShouldReturnFailure()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var result = await _sut.SendVerifyEmailAsync("u1", CancellationToken.None);

        result.Success.ShouldBeFalse();
    }

    #endregion

    #region FindGroupByNameAsync

    [Fact]
    public async Task FindGroupByNameAsync_ShouldReturnMatchingGroup()
    {
        var groupId = Guid.NewGuid().ToString();

        _handler.SetResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new[]
            {
                new { id = groupId, name = "Companies" }
            }));

        var result = await _sut.FindGroupByNameAsync("Companies", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(groupId);
        result.Name.ShouldBe("Companies");

        _handler.LastRequest!.RequestUri!.ToString().ShouldContain("search=Companies");
        _handler.LastRequest.RequestUri.ToString().ShouldContain("exact=true");
    }

    [Fact]
    public async Task FindGroupByNameAsync_WhenNoMatch_ShouldReturnNull()
    {
        _handler.SetResponse(HttpStatusCode.OK, "[]");

        var result = await _sut.FindGroupByNameAsync("NonExistent", CancellationToken.None);

        result.ShouldBeNull();
    }

    #endregion

    #region FindUserByEmailAsync

    [Fact]
    public async Task FindUserByEmailAsync_ShouldReturnMatchingUser()
    {
        var userId = Guid.NewGuid().ToString();

        _handler.SetResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new[]
            {
                new { id = userId, email = "jane@test.com", firstName = "Jane", lastName = "Doe" }
            }));

        var result = await _sut.FindUserByEmailAsync("jane@test.com", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(userId);

        _handler.LastRequest!.RequestUri!.ToString().ShouldContain("email=jane%40test.com");
        _handler.LastRequest.RequestUri.ToString().ShouldContain("exact=true");
    }

    [Fact]
    public async Task FindUserByEmailAsync_WhenNoMatch_ShouldReturnNull()
    {
        _handler.SetResponse(HttpStatusCode.OK, "[]");

        var result = await _sut.FindUserByEmailAsync("nobody@test.com", CancellationToken.None);

        result.ShouldBeNull();
    }

    #endregion
}
