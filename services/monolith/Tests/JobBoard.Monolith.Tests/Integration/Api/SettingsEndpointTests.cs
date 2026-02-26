using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.API.Helpers;
using JobBoard.Monolith.Contracts.Settings;
using JobBoard.Monolith.Tests.Integration.Fixtures;
using Shouldly;

namespace JobBoard.Monolith.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("Database")]
public class SettingsEndpointTests : IAsyncLifetime
{
    private readonly JobBoardWebApplicationFactory _factory;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public SettingsEndpointTests(TestDatabaseFixture dbFixture)
    {
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
    public async Task GetProvider_Returns200WithProviderSettings()
    {
        var response = await _client.GetAsync("/settings/provider");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ApiResponse<ProviderSettings>>(JsonOptions);
        json.ShouldNotBeNull();
        json.Success.ShouldBeTrue();
        json.Data.ShouldNotBeNull();
        json.Data.Provider.ShouldBe("openai");
        json.Data.Model.ShouldBe("gpt-4.1-mini");
    }

    [Fact]
    public async Task GetApplicationMode_Returns200WithModeDto()
    {
        var response = await _client.GetAsync("/settings/mode");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationModeDto>>(JsonOptions);
        json.ShouldNotBeNull();
        json.Success.ShouldBeTrue();
        json.Data.ShouldNotBeNull();
        json.Data.IsMonolith.ShouldBeTrue();
    }
}
