using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.API.Helpers;
using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Domain.Entities;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Monolith.Contracts.Companies;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class CompaniesEndpointTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly JobBoardWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public CompaniesEndpointTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new JobBoardWebApplicationFactory(dbFixture);
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        // Seed an industry for creating companies
        await using var ctx = _dbFixture.CreateContext();
        // Check if test industry already exists
        if (!ctx.Industries.Any(i => i.Name == "IntegrationTestIndustry"))
        {
            var (internalId, id) = await ctx.GetNextValueFromSequenceAsync(typeof(Industry), CancellationToken.None);
            var industry = Industry.Create("IntegrationTestIndustry");
            industry.InternalId = internalId;
            industry.Id = id;
            industry.CreatedAt = DateTime.UtcNow;
            industry.CreatedBy = "seed";
            industry.UpdatedAt = DateTime.UtcNow;
            industry.UpdatedBy = "seed";
            ctx.Industries.Add(industry);
            await ctx.SaveChangesAsync("seed", CancellationToken.None);
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<Guid> GetTestIndustryIdAsync()
    {
        await using var ctx = _dbFixture.CreateContext();
        var industry = ctx.Industries.First(i => i.Name == "IntegrationTestIndustry");
        return industry.Id;
    }

    [Fact]
    public async Task PostCompany_WithValidPayload_Returns201WithCompanyDto()
    {
        var industryId = await GetTestIndustryIdAsync();
        var suffix = Guid.NewGuid().ToString()[..8];
        var command = new CreateCompanyCommand
        {
            Name = $"ApiTestCorp-{suffix}",
            CompanyEmail = $"api-{suffix}@test.com",
            CompanyWebsite = "https://apitest.com",
            IndustryUId = industryId,
            AdminFirstName = "Api",
            AdminLastName = "Tester",
            AdminEmail = $"admin-{suffix}@test.com"
        };

        var response = await _client.PostAsJsonAsync("/companies", command);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            });

        json.ShouldNotBeNull();
        json.Success.ShouldBeTrue();
        json.Data.ShouldNotBeNull();
        json.Data.Name.ShouldBe(command.Name);
        json.Data.Email.ShouldBe(command.CompanyEmail);
        json.Data.Status.ShouldBe("Provisioning");
    }

    [Fact]
    public async Task PostCompany_WithMissingName_Returns400WithValidationErrors()
    {
        var industryId = await GetTestIndustryIdAsync();
        var command = new CreateCompanyCommand
        {
            Name = "", // Invalid - empty
            CompanyEmail = "valid@test.com",
            IndustryUId = industryId,
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@test.com"
        };

        var response = await _client.PostAsJsonAsync("/companies", command);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCompany_WithoutAuth_Returns401()
    {
        var industryId = await GetTestIndustryIdAsync();
        var command = new CreateCompanyCommand
        {
            Name = "UnauthorizedCorp",
            CompanyEmail = "unauth@test.com",
            IndustryUId = industryId,
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@test.com"
        };

        // Send request with anonymous header to trigger no-auth
        var request = new HttpRequestMessage(HttpMethod.Post, "/companies")
        {
            Content = JsonContent.Create(command)
        };
        request.Headers.Add("X-Anonymous", "true");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCompany_WithDuplicateName_Returns400()
    {
        var industryId = await GetTestIndustryIdAsync();
        var suffix = Guid.NewGuid().ToString()[..8];
        var uniqueName = $"DupNameCorp-{suffix}";

        // Create the first company
        var command1 = new CreateCompanyCommand
        {
            Name = uniqueName,
            CompanyEmail = $"first-{suffix}@test.com",
            IndustryUId = industryId,
            AdminFirstName = "First",
            AdminLastName = "Admin",
            AdminEmail = $"admin1-{suffix}@test.com"
        };
        var response1 = await _client.PostAsJsonAsync("/companies", command1);
        response1.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Attempt to create a second company with the same name
        var command2 = new CreateCompanyCommand
        {
            Name = uniqueName,
            CompanyEmail = $"second-{suffix}@test.com",
            IndustryUId = industryId,
            AdminFirstName = "Second",
            AdminLastName = "Admin",
            AdminEmail = $"admin2-{suffix}@test.com"
        };
        var response2 = await _client.PostAsJsonAsync("/companies", command2);

        response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCompany_WithDuplicateEmail_Returns400()
    {
        var industryId = await GetTestIndustryIdAsync();
        var suffix = Guid.NewGuid().ToString()[..8];
        var uniqueEmail = $"dup-{suffix}@test.com";

        // Create the first company
        var command1 = new CreateCompanyCommand
        {
            Name = $"FirstCorp-{suffix}",
            CompanyEmail = uniqueEmail,
            IndustryUId = industryId,
            AdminFirstName = "First",
            AdminLastName = "Admin",
            AdminEmail = $"admin1-{suffix}@test.com"
        };
        var response1 = await _client.PostAsJsonAsync("/companies", command1);
        response1.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Attempt to create a second company with the same email
        var command2 = new CreateCompanyCommand
        {
            Name = $"SecondCorp-{suffix}",
            CompanyEmail = uniqueEmail,
            IndustryUId = industryId,
            AdminFirstName = "Second",
            AdminLastName = "Admin",
            AdminEmail = $"admin2-{suffix}@test.com"
        };
        var response2 = await _client.PostAsJsonAsync("/companies", command2);

        response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
