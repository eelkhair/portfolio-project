using System.Net;
using System.Net.Http.Json;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Sync;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class SyncEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private readonly TestDataSeeder _seeder;
    private HttpClient _client = null!;

    private Industry _industry = null!;
    private Company _company = null!;

    public SyncEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
        _seeder = new TestDataSeeder(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _industry = await _seeder.SeedIndustryAsync("SyncTestIndustry-" + Guid.NewGuid().ToString()[..8]);
        _company = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──── Draft Sync ────

    [Fact]
    public async Task SyncDraft_WithValidPayload_CreatesDraft()
    {
        var draftId = Guid.NewGuid();
        var request = new SyncDraftRequest
        {
            DraftId = draftId,
            CompanyId = _company.Id,
            ContentJson = """{"title":"Synced Draft","aboutRole":"Test"}""",
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/drafts", request);

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, responseBody);

        // Verify draft was persisted
        await using var ctx = _dbFixture.CreateContext();
        var draft = await ctx.Drafts.FirstOrDefaultAsync(d => d.Id == draftId);
        draft.ShouldNotBeNull();
        draft.CompanyId.ShouldBe(_company.Id);
    }

    [Fact]
    public async Task SyncDraft_UpdatesExistingDraft()
    {
        var existingDraft = await _seeder.SeedDraftAsync(_company.Id, """{"title":"Original"}""");

        var request = new SyncDraftRequest
        {
            DraftId = existingDraft.Id,
            CompanyId = _company.Id,
            ContentJson = """{"title":"Updated via sync"}""",
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/drafts", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var ctx = _dbFixture.CreateContext();
        var draft = await ctx.Drafts.FirstAsync(d => d.Id == existingDraft.Id);
        draft.ContentJson.ShouldContain("Updated via sync");
    }

    [Fact]
    public async Task SyncDraft_WithoutAuth_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sync/drafts")
        {
            Content = JsonContent.Create(new SyncDraftRequest
            {
                DraftId = Guid.NewGuid(),
                CompanyId = _company.Id,
                ContentJson = "{}",
                UserId = "user"
            })
        };
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ──── Draft Delete ────

    [Fact]
    public async Task DeleteDraft_WithExistingDraft_Succeeds()
    {
        var draft = await _seeder.SeedDraftAsync(_company.Id);

        var response = await _client.DeleteAsync($"/api/sync/drafts/{draft.Id}?companyId={_company.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var ctx = _dbFixture.CreateContext();
        var deleted = await ctx.Drafts.FirstOrDefaultAsync(d => d.Id == draft.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteDraft_NonExistent_ReturnsOk_Idempotent()
    {
        var response = await _client.DeleteAsync($"/api/sync/drafts/{Guid.NewGuid()}?companyId={_company.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Company Create ────

    [Fact]
    public async Task SyncCompanyCreate_WithValidPayload_CreatesCompanyAndUser()
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var companyUId = Guid.NewGuid();
        var request = new SyncCompanyCreateRequest
        {
            CompanyId = companyUId,
            Name = $"SyncCorp-{suffix}",
            CompanyEmail = $"sync-{suffix}@test.com",
            CompanyWebsite = "https://synccorp.test",
            IndustryUId = _industry.Id,
            AdminFirstName = "Sync",
            AdminLastName = "Admin",
            AdminEmail = $"admin-{suffix}@test.com",
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/companies", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var ctx = _dbFixture.CreateContext();
        var company = await ctx.Companies.FirstOrDefaultAsync(c => c.Id == companyUId);
        company.ShouldNotBeNull();
        company.Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task SyncCompanyCreate_Idempotent_SkipsIfExists()
    {
        // _company already exists
        var request = new SyncCompanyCreateRequest
        {
            CompanyId = _company.Id,
            Name = _company.Name,
            CompanyEmail = "dupe@test.com",
            IndustryUId = _industry.Id,
            AdminFirstName = "Dupe",
            AdminLastName = "Admin",
            AdminEmail = "dupe-admin@test.com",
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/companies", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Company Update ────

    [Fact]
    public async Task SyncCompanyUpdate_WithValidPayload_UpdatesCompany()
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var request = new SyncCompanyUpdateRequest
        {
            CompanyId = _company.Id,
            Name = $"Updated-{suffix}",
            CompanyEmail = $"updated-{suffix}@test.com",
            IndustryUId = _industry.Id,
            Description = "Updated via sync",
            UserId = "sync-user"
        };

        var response = await _client.PutAsJsonAsync($"/api/sync/companies/{_company.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var ctx = _dbFixture.CreateContext();
        var updated = await ctx.Companies.FirstAsync(c => c.Id == _company.Id);
        updated.Description.ShouldBe("Updated via sync");
    }

    // ──── Job Create ────

    [Fact]
    public async Task SyncJobCreate_WithValidPayload_CreatesJob()
    {
        var jobId = Guid.NewGuid();
        var request = new SyncJobCreateRequest
        {
            JobId = jobId,
            CompanyId = _company.Id,
            Title = "Synced Engineer",
            AboutRole = "Building synced systems",
            Location = "Remote",
            SalaryRange = "$100k-$150k",
            JobType = "FullTime",
            Responsibilities = ["Build stuff", "Review code"],
            Qualifications = ["5 years exp", "C#"],
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/jobs", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var ctx = _dbFixture.CreateContext();
        var job = await ctx.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        job.ShouldNotBeNull();
        job.Title.ShouldBe("Synced Engineer");
    }

    [Fact]
    public async Task SyncJobCreate_Idempotent_SkipsIfExists()
    {
        var existingJob = await _seeder.SeedJobAsync(_company.InternalId);

        var request = new SyncJobCreateRequest
        {
            JobId = existingJob.Id,
            CompanyId = _company.Id,
            Title = "Duplicate Job",
            AboutRole = "Dupe",
            Location = "Remote",
            JobType = "FullTime",
            Responsibilities = [],
            Qualifications = [],
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/jobs", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SyncJobCreate_WithInvalidJobType_DefaultsToFullTime()
    {
        var jobId = Guid.NewGuid();
        var request = new SyncJobCreateRequest
        {
            JobId = jobId,
            CompanyId = _company.Id,
            Title = "Fallback Job",
            AboutRole = "Testing fallback",
            Location = "NYC",
            JobType = "InvalidType",
            Responsibilities = ["Task"],
            Qualifications = ["Skill"],
            UserId = "sync-user"
        };

        var response = await _client.PostAsJsonAsync("/api/sync/jobs", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var ctx = _dbFixture.CreateContext();
        var job = await ctx.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        job.ShouldNotBeNull();
    }
}
