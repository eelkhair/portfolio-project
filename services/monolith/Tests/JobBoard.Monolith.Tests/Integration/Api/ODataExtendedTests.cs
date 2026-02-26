using System.Net;
using System.Text.Json;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class ODataExtendedTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ODataExtendedTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var ctx = _dbFixture.CreateContext();

        // Seed industries and companies for ordering/paging tests
        if (!ctx.Industries.Any(i => i.Name == "ODataExtIndustry"))
        {
            var (indId, indUid) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
            var industry = Industry.Create("ODataExtIndustry");
            industry.InternalId = indId;
            industry.Id = indUid;
            industry.CreatedAt = DateTime.UtcNow;
            industry.CreatedBy = "seed";
            industry.UpdatedAt = DateTime.UtcNow;
            industry.UpdatedBy = "seed";
            ctx.Industries.Add(industry);
            await ctx.SaveChangesAsync("seed", CancellationToken.None);

            // Create two companies for ordering
            var names = new[] { "Zebra Corp", "Alpha Corp" };
            foreach (var name in names)
            {
                var (compId, compUid) = await ctx.GetNextValueFromSequenceAsync(typeof(Company), CancellationToken.None);
                var company = Company.Create(new CompanyInput(
                    InternalId: compId, Id: compUid,
                    Name: name, Email: $"{name.ToLower().Replace(" ", "")}@test.com",
                    Status: "Active", IndustryId: indId,
                    CreatedAt: DateTime.UtcNow, CreatedBy: "seed"));
                ctx.Companies.Add(company);
            }

            await ctx.SaveChangesAsync("seed", CancellationToken.None);
        }

        // Seed a user for /odata/users test
        if (!ctx.Users.Any(u => u.Email == "odata-ext@test.com"))
        {
            var (userId, userUid) = await ctx.GetNextValueFromSequenceAsync(typeof(User), CancellationToken.None);
            var user = User.Create("OData", "TestUser", "odata-ext@test.com", "ext-odata",
                userUid, userId, DateTime.UtcNow, "seed");
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "seed";
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync("seed", CancellationToken.None);
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetCompanies_WithOrderBy_ReturnsOrderedResults()
    {
        var response = await _client.GetAsync("/odata/companies?$orderby=name");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();

        // Alpha should appear before Zebra when ordered by name
        var alphaIndex = content.IndexOf("Alpha Corp", StringComparison.Ordinal);
        var zebraIndex = content.IndexOf("Zebra Corp", StringComparison.Ordinal);
        if (alphaIndex >= 0 && zebraIndex >= 0)
        {
            alphaIndex.ShouldBeLessThan(zebraIndex);
        }
    }

    [Fact]
    public async Task GetCompanies_WithTop_ReturnsLimitedResults()
    {
        var response = await _client.GetAsync("/odata/companies?$top=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();

        // Parse OData response to verify only 1 result
        var doc = JsonDocument.Parse(content);
        var values = doc.RootElement.GetProperty("value");
        values.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task GetCompanies_WithCount_IncludesCount()
    {
        var response = await _client.GetAsync("/odata/companies?$count=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();

        // OData $count=true adds @odata.count to the response
        var doc = JsonDocument.Parse(content);
        doc.RootElement.TryGetProperty("@odata.count", out var countElement).ShouldBeTrue();
        countElement.GetInt32().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetUsers_ReturnsUserData()
    {
        var response = await _client.GetAsync("/odata/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();
        // Should contain user data (at minimum the test auth user synced by middleware)
        content.ShouldContain("value");
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/odata/users");
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
