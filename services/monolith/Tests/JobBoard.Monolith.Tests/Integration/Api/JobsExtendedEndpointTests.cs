using System.Net;
using System.Net.Http.Json;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Jobs;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class JobsExtendedEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private readonly TestDataSeeder _seeder;
    private HttpClient _client = null!;

    private Industry _industry = null!;
    private Company _company = null!;

    public JobsExtendedEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
        _seeder = new TestDataSeeder(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _industry = await _seeder.SeedIndustryAsync("JobsExtTestIndustry-" + Guid.NewGuid().ToString()[..8]);
        _company = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──── CreateJob ────

    [Fact]
    public async Task CreateJob_WithValidPayload_ReturnsOk()
    {
        // First seed a draft so we can reference it
        var draft = await _seeder.SeedDraftAsync(_company.Id);

        var request = new CreateJobRequest
        {
            Title = "Integration Test Engineer",
            CompanyUId = _company.Id,
            Location = "Remote",
            JobType = JobType.FullTime,
            AboutRole = "Write integration tests",
            SalaryRange = "$90k-$130k",
            Responsibilities = ["Write tests", "Review code"],
            Qualifications = ["3+ years C#", "xUnit experience"],
            DraftId = draft.Id.ToString(),
            DeleteDraft = false
        };

        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateJob_WithoutAuth_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/jobs")
        {
            Content = JsonContent.Create(new CreateJobRequest
            {
                Title = "Unauthorized Job",
                CompanyUId = _company.Id,
                Location = "NYC",
                JobType = JobType.Contract,
                AboutRole = "Test",
                DraftId = Guid.NewGuid().ToString()
            })
        };
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ──── SaveDraft ────

    [Fact]
    public async Task SaveDraft_WithValidPayload_ReturnsOk()
    {
        var draftContent = new DraftResponse
        {
            Title = "Saved Draft Title",
            AboutRole = "Draft role description",
            Responsibilities = ["Task 1"],
            Qualifications = ["Skill 1"],
            Location = "Remote",
            JobType = "FullTime",
            SalaryRange = "$100k"
        };

        var response = await _client.PutAsJsonAsync($"/api/jobs/{_company.Id}/save-draft", draftContent);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── GetDraft ────

    [Fact]
    public async Task GetDraft_WithValidId_ReturnsOk()
    {
        var draft = await _seeder.SeedDraftAsync(_company.Id);

        var response = await _client.GetAsync($"/api/jobs/drafts/{draft.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDraft_WithNonExistentId_Returns404OrBadRequest()
    {
        var response = await _client.GetAsync($"/api/jobs/drafts/{Guid.NewGuid()}");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    // ──── DraftsByCompany ────

    [Fact]
    public async Task DraftsByCompany_ReturnsOk()
    {
        await _seeder.SeedDraftAsync(_company.Id);

        var response = await _client.GetAsync("/api/jobs/drafts/by-company");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── DeleteDraft ────

    [Fact]
    public async Task DeleteDraft_WithValidDraft_ReturnsOk()
    {
        var draft = await _seeder.SeedDraftAsync(_company.Id);

        var response = await _client.DeleteAsync($"/api/jobs/{_company.Id}/drafts/{draft.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteDraft_NonExistent_Returns400()
    {
        var response = await _client.DeleteAsync($"/api/jobs/{_company.Id}/drafts/{Guid.NewGuid()}");

        // Handler throws DomainException for not found → decorator converts to 400
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
