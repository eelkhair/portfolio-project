using JobBoard.Domain;
using JobBoard.Infrastructure.Configuration.Services;
using Microsoft.FeatureManagement;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class FeatureFlagServiceTests
{
    private readonly IFeatureManager _featureManager;
    private readonly FeatureFlagService _sut;

    public FeatureFlagServiceTests()
    {
        _featureManager = Substitute.For<IFeatureManager>();
        _sut = new FeatureFlagService(_featureManager);
    }

    [Fact]
    public async Task IsEnabledAsync_ShouldDelegateToFeatureManager()
    {
        _featureManager.IsEnabledAsync("EnableEfTracking")
            .Returns(true);

        var result = await _sut.IsEnabledAsync(FeatureFlags.EnableEfTracking);

        result.ShouldBeTrue();
        await _featureManager.Received(1).IsEnabledAsync("EnableEfTracking");
    }

    [Fact]
    public async Task IsEnabledAsync_WhenDisabled_ShouldReturnFalse()
    {
        _featureManager.IsEnabledAsync("EnableEfTracking")
            .Returns(false);

        var result = await _sut.IsEnabledAsync(FeatureFlags.EnableEfTracking);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllFeaturesAsync_ShouldReturnAllFeatureFlags()
    {
        _featureManager.IsEnabledAsync("EnableEfTracking")
            .Returns(true);

        var result = await _sut.GetAllFeaturesAsync();

        result.ShouldContainKey("EnableEfTracking");
        result["EnableEfTracking"].ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllFeaturesAsync_ShouldEnumerateAllEnumValues()
    {
        _featureManager.IsEnabledAsync(Arg.Any<string>())
            .Returns(false);

        var result = await _sut.GetAllFeaturesAsync();

        var expectedCount = Enum.GetNames<FeatureFlags>().Length;
        result.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public async Task GetAllFeaturesAsync_ShouldCallFeatureManagerForEachFlag()
    {
        _featureManager.IsEnabledAsync(Arg.Any<string>())
            .Returns(false);

        await _sut.GetAllFeaturesAsync();

        var expectedCount = Enum.GetNames<FeatureFlags>().Length;
        await _featureManager.Received(expectedCount).IsEnabledAsync(Arg.Any<string>());
    }
}
