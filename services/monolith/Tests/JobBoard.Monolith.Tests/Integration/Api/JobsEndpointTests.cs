using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.API.Helpers;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class JobsEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public JobsEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
    }

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GenerateDraft_WithValidRequest_Returns200()
    {
        var companyId = Guid.NewGuid();
        var request = new DraftGenRequest
        {
            Brief = "Software Engineer role",
            RoleLevel = RoleLevel.Senior,
            Tone = Tone.Neutral,
            MaxBullets = 5
        };

        var response = await _client.PostAsJsonAsync($"/jobs/{companyId}/generate", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ApiResponse<DraftGenResponse>>(JsonOptions);
        json.ShouldNotBeNull();
        json.Success.ShouldBeTrue();
        json.Data.ShouldNotBeNull();
        json.Data.Title.ShouldBe("Test Draft");
        json.Data.DraftId.ShouldBe("draft-1");
    }

    [Fact]
    public async Task ListDrafts_WithValidCompanyId_Returns200()
    {
        var companyId = Guid.NewGuid();

        var response = await _client.GetAsync($"/jobs/{companyId}/list-drafts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ApiResponse<List<DraftResponse>>>(JsonOptions);
        json.ShouldNotBeNull();
        json.Success.ShouldBeTrue();
        json.Data.ShouldNotBeNull();
        json.Data.Count.ShouldBe(1);
        json.Data[0].Title.ShouldBe("Test Draft");
    }

    [Fact]
    public async Task RewriteDraftItem_WithValidRequest_Returns200()
    {
        var request = new DraftItemRewriteRequest
        {
            Field = "title",
            Value = "Original Title",
            Context = new Dictionary<string, object> { ["role"] = "Engineer" },
            Style = new Dictionary<string, object> { ["tone"] = "professional" }
        };

        var response = await _client.PutAsJsonAsync("/jobs/drafts/rewrite", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ApiResponse<DraftRewriteResponse>>(JsonOptions);
        json.ShouldNotBeNull();
        json.Success.ShouldBeTrue();
        json.Data.ShouldNotBeNull();
        json.Data.Field.ShouldBe("title");
        json.Data.Options.ShouldContain("Option A");
    }

    [Fact]
    public async Task GenerateDraft_WithoutAuth_Returns401()
    {
        var companyId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/jobs/{companyId}/generate")
        {
            Content = JsonContent.Create(new DraftGenRequest { Brief = "Test" })
        };
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListDrafts_WithoutAuth_Returns401()
    {
        var companyId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/jobs/{companyId}/list-drafts");
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
