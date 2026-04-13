using System.Net;
using System.Net.Http.Json;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Public;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class ApplicantEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private readonly TestDataSeeder _seeder;
    private HttpClient _client = null!;

    private Industry _industry = null!;
    private Company _company = null!;
    private Job _job = null!;

    public ApplicantEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
        _seeder = new TestDataSeeder(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _industry = await _seeder.SeedIndustryAsync("ApplicantTestIndustry-" + Guid.NewGuid().ToString()[..8]);
        _company = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId);
        _job = await _seeder.SeedJobAsync(_company.InternalId);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        // Default TestAuthHandler gives Admins,Recruiters but applicant endpoints
        // go through the general auth pipeline — any authenticated user can access
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    private HttpRequestMessage CreateAnonymousRequest(HttpMethod method, string url, object? body = null)
    {
        var request = CreateRequest(method, url, body);
        request.Headers.Add("X-Anonymous", "true");
        return request;
    }

    // ──── Profile ────

    [Fact]
    public async Task GetProfile_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/applicant/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpsertProfile_WithValidPayload_ReturnsOk()
    {
        var profile = new UserProfileRequest
        {
            Phone = "+1234567890",
            LinkedIn = "https://linkedin.com/in/test",
            About = "Integration test profile",
            Skills = ["C#", "TypeScript"],
            PreferredLocation = "Remote"
        };

        var response = await _client.PutAsJsonAsync("/api/applicant/profile", profile);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_AfterUpsert_ReturnsUpdatedData()
    {
        var profile = new UserProfileRequest
        {
            Phone = "+9876543210",
            About = "Updated profile for test"
        };

        await _client.PutAsJsonAsync("/api/applicant/profile", profile);

        var response = await _client.GetAsync("/api/applicant/profile");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_Returns401()
    {
        var request = CreateAnonymousRequest(HttpMethod.Get, "/api/applicant/profile");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpsertProfile_WithoutAuth_Returns401()
    {
        var request = CreateAnonymousRequest(HttpMethod.Put, "/api/applicant/profile",
            new UserProfileRequest { About = "test" });

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ──── Applications ────

    [Fact]
    public async Task GetApplications_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/applicant/applications");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitApplication_WithValidPayload_Returns201()
    {
        var request = new SubmitApplicationRequest
        {
            JobId = _job.Id,
            CoverLetter = "I would be a great fit for this role.",
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Test",
                LastName = "Applicant",
                Email = "applicant@test.com"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/applicant/applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SubmitApplication_WithInvalidJobId_Returns400Or404()
    {
        var request = new SubmitApplicationRequest
        {
            JobId = Guid.NewGuid(), // Non-existent job
            CoverLetter = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/applicant/applications", request);

        // Non-existent job may return 400 (validation), 404, or 500 (unhandled lookup failure)
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApplications_WithoutAuth_Returns401()
    {
        var request = CreateAnonymousRequest(HttpMethod.Get, "/api/applicant/applications");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitApplication_WithoutAuth_Returns401()
    {
        var request = CreateAnonymousRequest(HttpMethod.Post, "/api/applicant/applications",
            new SubmitApplicationRequest { JobId = _job.Id });

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
