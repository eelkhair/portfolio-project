using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Public;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class ResumesEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private readonly TestDataSeeder _seeder;
    private HttpClient _client = null!;

    private Resume _resume = null!;
    private User _user = null!;

    public ResumesEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
        _seeder = new TestDataSeeder(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        // The default test user is "test-user-id" (external ID).
        // We need to ensure a User entity exists in DB for the UserContextDecorator.
        // The decorator auto-syncs the user, so the first request will create it.
        // But for seeding resumes, we need a user with a known InternalId.
        _user = await _seeder.SeedUserAsync("Resume", "Tester", "test-user-id");
        _resume = await _seeder.SeedResumeAsync(_user.InternalId, isDefault: true);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──── Upload ────

    [Fact]
    public async Task UploadResume_WithValidFile_Returns201()
    {
        using var content = new MultipartFormDataContent();
        var fileBytes = "fake-pdf-content"u8.ToArray();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "resume.pdf");

        var response = await _client.PostAsync("/api/resumes", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UploadResume_WithoutAuth_Returns401()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([1, 2, 3]), "file", "resume.pdf");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/resumes") { Content = content };
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ──── List ────

    [Fact]
    public async Task GetResumes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/resumes");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Parsed Content ────

    [Fact]
    public async Task GetParsedContent_WithValidResume_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/resumes/{_resume.Id}/parsed-content");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetParsedContentInternal_WithValidResume_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/resumes/{_resume.Id}/parsed-content/internal");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Download ────

    [Fact]
    public async Task DownloadResume_WithValidId_ReturnsFile()
    {
        var response = await _client.GetAsync($"/api/resumes/{_resume.Id}/download");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task DownloadResume_Inline_ReturnsFile()
    {
        var response = await _client.GetAsync($"/api/resumes/{_resume.Id}/download?inline=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DownloadResume_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/api/resumes/{Guid.NewGuid()}/download");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ──── SetDefault ────

    [Fact]
    public async Task SetDefaultResume_ReturnsNoContent()
    {
        var response = await _client.PatchAsync($"/api/resumes/{_resume.Id}/default", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ──── Delete ────

    [Fact]
    public async Task DeleteResume_WithValidId_ReturnsNoContent()
    {
        var toDelete = await _seeder.SeedResumeAsync(_user.InternalId);

        var response = await _client.DeleteAsync($"/api/resumes/{toDelete.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ──── Parse Callbacks ────

    [Fact]
    public async Task ParseCompleted_WithValidPayload_ReturnsOk()
    {
        var request = new ResumeParseCompletedModel
        {
            ResumeUId = _resume.Id,
            UserId = "test-user-id",
            CurrentPage = "/resumes",
            ParsedContent = new ResumeParsedContentResponse
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                Skills = ["C#", "TypeScript"]
            }
        };

        var response = await _client.PostAsJsonAsync("/api/resumes/parse-completed", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ParseFailed_WithValidPayload_ReturnsOk()
    {
        var failResume = await _seeder.SeedResumeAsync(_user.InternalId);

        var request = new ResumeParseFailedModel
        {
            ResumeUId = failResume.Id,
            UserId = "test-user-id",
            CurrentPage = "/resumes",
            Reason = "Could not parse PDF"
        };

        var response = await _client.PostAsJsonAsync("/api/resumes/parse-failed", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SectionParsed_WithValidPayload_ReturnsOk()
    {
        var request = new ResumeSectionParsedModel
        {
            ResumeUId = _resume.Id,
            UserId = "test-user-id",
            Section = "skills",
            SectionContent = JsonSerializer.SerializeToElement(new[] { "C#", "TypeScript" }),
            CurrentPage = "/resumes"
        };

        var response = await _client.PostAsJsonAsync("/api/resumes/section-parsed", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SectionFailed_WithValidPayload_ReturnsOk()
    {
        var request = new ResumeSectionFailedModel
        {
            ResumeUId = _resume.Id,
            UserId = "test-user-id",
            Section = "education",
            Reason = "Could not extract education",
            CurrentPage = "/resumes"
        };

        var response = await _client.PostAsJsonAsync("/api/resumes/section-failed", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AllSectionsCompleted_WithValidPayload_ReturnsOk()
    {
        var request = new ResumeAllSectionsCompletedModel
        {
            ResumeUId = _resume.Id,
            UserId = "test-user-id",
            CurrentPage = "/resumes"
        };

        var response = await _client.PostAsJsonAsync("/api/resumes/all-sections-completed", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Matching Jobs ────

    [Fact]
    public async Task GetMatchingJobs_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/resumes/jobs/matching");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──── Re-embed ────

    [Fact]
    public async Task ReEmbed_UnparsedResume_ReturnsError()
    {
        // Seeded resume has no parsed content, so re-embed should fail
        var response = await _client.PostAsync($"/api/resumes/{_resume.Id}/re-embed", null);

        // 500 because InvalidOperationException is unhandled, or 400 if guarded
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }
}
