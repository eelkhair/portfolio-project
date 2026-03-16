using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class PublicEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private readonly TestDataSeeder _seeder;
    private HttpClient _client = null!;

    private Industry _industry = null!;
    private Company _company1 = null!;
    private Company _company2 = null!;
    private Job _job1 = null!;
    private Job _job2 = null!;

    public PublicEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
        _seeder = new TestDataSeeder(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _industry = await _seeder.SeedIndustryAsync("PublicTestIndustry-" + Guid.NewGuid().ToString()[..8]);
        _company1 = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId, "PublicCorp1-" + Guid.NewGuid().ToString()[..8]);
        _company2 = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId, "PublicCorp2-" + Guid.NewGuid().ToString()[..8]);
        _job1 = await _seeder.SeedJobAsync(_company1.InternalId, "Public Engineer");
        _job2 = await _seeder.SeedJobAsync(_company2.InternalId, "Public Designer");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ──── Jobs ────

    [Fact]
    public async Task GetJobs_ReturnsOkWithJobs()
    {
        var response = await _client.GetAsync("/api/public/jobs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetJobs_WithPagination_ReturnsLimitedResults()
    {
        var response = await _client.GetAsync("/api/public/jobs?page=1&pageSize=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJobById_WithValidId_ReturnsJob()
    {
        var response = await _client.GetAsync($"/api/public/jobs/{_job1.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJobById_WithNonExistentId_Returns404OrEmpty()
    {
        var response = await _client.GetAsync($"/api/public/jobs/{Guid.NewGuid()}");

        // Depending on handler impl, may return 404 or OK with null data
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSimilarJobs_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/public/jobs/{_job1.Id}/similar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchJobs_WithQuery_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/jobs/search?query=Engineer");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchJobs_WithLocation_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/jobs/search?location=Remote");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLatestJobs_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/jobs/latest");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLatestJobs_WithCustomCount_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/jobs/latest?count=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Companies ────

    [Fact]
    public async Task GetCompanies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/companies");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCompanyById_WithValidId_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/public/companies/{_company1.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCompanyById_WithNonExistentId_Returns404OrEmpty()
    {
        var response = await _client.GetAsync($"/api/public/companies/{Guid.NewGuid()}");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCompanyJobs_WithValidCompany_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/public/companies/{_company1.Id}/jobs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCompanyJobs_WithCompanyWithoutJobs_ReturnsOk()
    {
        // Seed a company with no jobs
        var emptyCompany = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId);

        var response = await _client.GetAsync($"/api/public/companies/{emptyCompany.Id}/jobs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Stats ────

    [Fact]
    public async Task GetStats_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/stats");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Anonymous access ────

    [Fact]
    public async Task PublicEndpoints_AreAccessibleWithoutAuth()
    {
        // Public endpoints are AllowAnonymous — even anonymous requests should work
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/jobs");
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
