using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Companies;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class ODataEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ODataEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        // Seed test data
        await using var ctx = _dbFixture.CreateContext();
        if (!ctx.Industries.Any(i => i.Name == "ODataTestIndustry"))
        {
            var (indId, indUid) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
            var industry = Industry.Create("ODataTestIndustry");
            industry.InternalId = indId;
            industry.Id = indUid;
            industry.CreatedAt = DateTime.UtcNow;
            industry.CreatedBy = "seed";
            industry.UpdatedAt = DateTime.UtcNow;
            industry.UpdatedBy = "seed";
            ctx.Industries.Add(industry);
            await ctx.SaveChangesAsync("seed", CancellationToken.None);

            var (compId, compUid) = await ctx.GetNextValueFromSequenceAsync(typeof(Company), CancellationToken.None);
            var company = Company.Create(new CompanyInput(
                InternalId: compId,
                Id: compUid,
                Name: "ODataTestCorp",
                Email: "odata@test.com",
                Status: "Active",
                IndustryId: indId,
                Website: "https://odata.test",
                CreatedAt: DateTime.UtcNow,
                CreatedBy: "seed"));
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync("seed", CancellationToken.None);
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetCompanies_ReturnsODataResponse()
    {
        var response = await _client.GetAsync("/odata/companies");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();

        // OData responses contain the data we seeded
        content.ShouldContain("ODataTestCorp");
    }

    [Fact]
    public async Task GetIndustries_ReturnsODataResponse()
    {
        var response = await _client.GetAsync("/odata/industries");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();
        content.ShouldContain("ODataTestIndustry");
    }

    [Fact]
    public async Task GetCompanies_WithSelectFilter_ReturnsFilteredFields()
    {
        var response = await _client.GetAsync("/odata/companies?$select=name,email");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("ODataTestCorp");
        content.ShouldContain("odata@test.com");
    }

    [Fact]
    public async Task GetCompanies_WithFilter_ReturnsMatchingCompanies()
    {
        var response = await _client.GetAsync("/odata/companies?$filter=name eq 'ODataTestCorp'");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("ODataTestCorp");
    }

    [Fact]
    public async Task GetCompanies_WithoutAuth_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/odata/companies");
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
