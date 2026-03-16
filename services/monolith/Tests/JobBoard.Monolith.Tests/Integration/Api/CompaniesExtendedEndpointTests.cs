using System.Net;
using System.Net.Http.Json;
using JobBoard.Application.Actions.Companies.Update;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class CompaniesExtendedEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private readonly TestDataSeeder _seeder;
    private HttpClient _client = null!;

    private Industry _industry = null!;
    private Company _company = null!;

    public CompaniesExtendedEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
        _seeder = new TestDataSeeder(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _industry = await _seeder.SeedIndustryAsync("CompExtTestIndustry-" + Guid.NewGuid().ToString()[..8]);
        _company = await _seeder.SeedActivatedCompanyAsync(_industry.InternalId);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UpdateCompany_WithValidPayload_ReturnsOk()
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var command = new UpdateCompanyCommand
        {
            Id = _company.Id,
            Name = $"UpdatedCorp-{suffix}",
            CompanyEmail = $"updated-{suffix}@test.com",
            IndustryUId = _industry.Id,
            Description = $"Updated description {suffix}",
            Phone = "+1234567890",
            About = "About this company"
        };

        var response = await _client.PutAsJsonAsync($"/api/companies/{_company.Id}", command);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCompany_NonExistent_Returns400Or404()
    {
        var command = new UpdateCompanyCommand
        {
            Id = Guid.NewGuid(),
            Description = "Test"
        };

        var response = await _client.PutAsJsonAsync($"/api/companies/{Guid.NewGuid()}", command);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCompany_WithoutAuth_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/companies/{_company.Id}")
        {
            Content = JsonContent.Create(new UpdateCompanyCommand
            {
                Id = _company.Id,
                Description = "Test"
            })
        };
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
