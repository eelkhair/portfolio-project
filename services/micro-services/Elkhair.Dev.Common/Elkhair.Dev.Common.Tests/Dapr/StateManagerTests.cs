using Dapr.Client;
using Elkhair.Dev.Common.Dapr;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Dapr;

[Trait("Category", "Unit")]
public class StateManagerTests
{
    private readonly ILogger<StateManager> _logger;
    private readonly DaprClient _daprClient;
    private readonly StateManager _sut;

    public StateManagerTests()
    {
        _logger = Substitute.For<ILogger<StateManager>>();
        _daprClient = Substitute.For<DaprClient>();
        _sut = new StateManager(_logger, _daprClient);
    }

    [Fact]
    public async Task SaveStateAsync_CallsDaprClient()
    {
        // Arrange
        var store = "statestore";
        var key = "test-key";
        var value = new { Name = "Test" };
        var ct = CancellationToken.None;

        // Act
        await _sut.SaveStateAsync(store, key, value, ct);

        // Assert
        await _daprClient.Received(1).SaveStateAsync(store, key, value, cancellationToken: ct);
    }

    [Fact]
    public async Task SaveStateAsync_LogsInformation()
    {
        // Arrange
        var store = "statestore";
        var key = "test-key";

        // Act
        await _sut.SaveStateAsync(store, key, "value", CancellationToken.None);

        // Assert
        _logger.ReceivedWithAnyArgs(1).LogInformation(default(string)!, default(object[])!);
    }

    [Fact]
    public async Task GetStateAsync_ReturnsDaprClientResult()
    {
        // Arrange
        var store = "statestore";
        var key = "test-key";
        var ct = CancellationToken.None;
        _daprClient.GetStateAsync<string>(store, key, cancellationToken: ct)
            .Returns("stored-value");

        // Act
        var result = await _sut.GetStateAsync<string>(store, key, ct);

        // Assert
        result.ShouldBe("stored-value");
    }

    [Fact]
    public async Task GetStateAsync_CallsDaprClient()
    {
        // Arrange
        var store = "statestore";
        var key = "test-key";
        var ct = CancellationToken.None;

        // Act
        await _sut.GetStateAsync<string>(store, key, ct);

        // Assert
        await _daprClient.Received(1).GetStateAsync<string>(store, key, cancellationToken: ct);
    }

    [Fact]
    public async Task DeleteStateAsync_CallsDaprClient()
    {
        // Arrange
        var store = "statestore";
        var key = "test-key";
        var ct = CancellationToken.None;

        // Act
        await _sut.DeleteStateAsync(store, key, ct);

        // Assert
        await _daprClient.Received(1).DeleteStateAsync(store, key, cancellationToken: ct);
    }

    [Fact]
    public async Task DeleteStateAsync_LogsInformation()
    {
        // Arrange
        var store = "statestore";
        var key = "test-key";

        // Act
        await _sut.DeleteStateAsync(store, key, CancellationToken.None);

        // Assert
        _logger.ReceivedWithAnyArgs(1).LogInformation(default(string)!, default(object[])!);
    }

    [Fact]
    public async Task QueryStateAsync_CallsDaprClient()
    {
        // Arrange
        var store = "statestore";
        var query = "{\"filter\":{}}";
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal) { { "key", "value" } };
        var ct = CancellationToken.None;

        // Act
        await _sut.QueryStateAsync<string>(store, query, metadata, ct);

        // Assert
        await _daprClient.Received(1).QueryStateAsync<string>(store, query, metadata, cancellationToken: ct);
    }

    [Fact]
    public async Task SaveStateAsync_WithCancellationToken_PassesTokenToDaprClient()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await _sut.SaveStateAsync("store", "key", "value", ct);

        // Assert
        await _daprClient.Received(1).SaveStateAsync("store", "key", "value", cancellationToken: ct);
    }

    [Fact]
    public async Task GetStateAsync_WithComplexType_ReturnsCorrectType()
    {
        // Arrange
        var expected = new List<string> { "a", "b", "c" };
        _daprClient.GetStateAsync<List<string>>("store", "key", cancellationToken: CancellationToken.None)
            .Returns(expected);

        // Act
        var result = await _sut.GetStateAsync<List<string>>("store", "key", CancellationToken.None);

        // Assert
        result.ShouldBe(expected);
        result.Count.ShouldBe(3);
    }
}
