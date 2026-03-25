using AdminApi.Features.Jobs.DraftGenerator;
using AdminAPI.Contracts.Models.Jobs;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Shouldly;

namespace AdminApi.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public class JobGenValidatorTests
{
    private readonly JobGenValidator _validator = new();

    private static JobGenRequest ValidRequest() => new()
    {
        Brief = "Build and own the payment processing pipeline",
        TitleSeed = "Senior Backend Engineer",
        RoleLevel = RoleLevel.Senior,
        Tone = Tone.Neutral,
        MaxBullets = 5
    };

    // ── Brief ──

    [Fact]
    public async Task Brief_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Brief = "";

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "Brief"
            && e.ErrorMessage == "Brief is required");
    }

    [Fact]
    public async Task Brief_WhenLessThan10Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Brief = "Too short";

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "Brief"
            && e.ErrorMessage == "Brief must be at least 10 characters");
    }

    [Fact]
    public async Task Brief_WhenExactly10Chars_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.Brief = "1234567890";

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Brief");
    }

    // ── TitleSeed ──

    [Fact]
    public async Task TitleSeed_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.TitleSeed = "";

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "TitleSeed"
            && e.ErrorMessage == "Title Seed is required");
    }

    [Fact]
    public async Task TitleSeed_WhenNull_ShouldHaveError()
    {
        var request = ValidRequest();
        request.TitleSeed = null;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "TitleSeed");
    }

    [Fact]
    public async Task TitleSeed_WhenProvided_ShouldNotHaveError()
    {
        var request = ValidRequest();

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "TitleSeed");
    }

    // ── RoleLevel ──

    [Fact]
    public async Task RoleLevel_WhenValidEnum_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.RoleLevel = RoleLevel.Staff;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "RoleLevel");
    }

    [Fact]
    public async Task RoleLevel_WhenInvalidEnum_ShouldHaveError()
    {
        var request = ValidRequest();
        request.RoleLevel = (RoleLevel)99;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "RoleLevel");
    }

    // ── Tone ──

    [Fact]
    public async Task Tone_WhenValidEnum_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.Tone = Tone.Friendly;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Tone");
    }

    [Fact]
    public async Task Tone_WhenInvalidEnum_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Tone = (Tone)99;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "Tone");
    }

    // ── MaxBullets ──

    [Fact]
    public async Task MaxBullets_WhenBelowMinimum_ShouldHaveError()
    {
        var request = ValidRequest();
        request.MaxBullets = 2;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "MaxBullets");
    }

    [Fact]
    public async Task MaxBullets_WhenAboveMaximum_ShouldHaveError()
    {
        var request = ValidRequest();
        request.MaxBullets = 9;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "MaxBullets");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public async Task MaxBullets_WhenInRange_ShouldNotHaveError(int bullets)
    {
        var request = ValidRequest();
        request.MaxBullets = bullets;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "MaxBullets");
    }

    // ── Location ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Remote")]
    [InlineData("remote")]
    [InlineData("Hybrid")]
    [InlineData("hybrid")]
    [InlineData("New York, NY")]
    [InlineData("San Francisco, CA")]
    public async Task Location_WhenValidFormat_ShouldNotHaveError(string? location)
    {
        var request = ValidRequest();
        request.Location = location;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Location");
    }

    [Theory]
    [InlineData("Invalid Location Format")]
    [InlineData("Just A City")]
    [InlineData("123")]
    public async Task Location_WhenInvalidFormat_ShouldHaveError(string location)
    {
        var request = ValidRequest();
        request.Location = location;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "Location");
    }

    // ── CSV fields max length ──

    [Fact]
    public async Task TechStackCSV_WhenExceeds2000Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.TechStackCSV = new string('x', 2001);

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "TechStackCSV");
    }

    [Fact]
    public async Task MustHavesCSV_WhenExceeds1000Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.MustHavesCSV = new string('x', 1001);

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "MustHavesCSV");
    }

    [Fact]
    public async Task NiceToHavesCSV_WhenExceeds1000Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.NiceToHavesCSV = new string('x', 1001);

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "NiceToHavesCSV");
    }

    [Fact]
    public async Task CSVFields_WhenWithinLimits_ShouldNotHaveErrors()
    {
        var request = ValidRequest();
        request.TechStackCSV = "C#, .NET, SQL";
        request.MustHavesCSV = "5 years experience";
        request.NiceToHavesCSV = "Cloud experience";

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "TechStackCSV");
        result.Errors.ShouldNotContain(e => e.PropertyName == "MustHavesCSV");
        result.Errors.ShouldNotContain(e => e.PropertyName == "NiceToHavesCSV");
    }

    // ── Valid request ──

    [Fact]
    public async Task ValidRequest_ShouldPassAllRules()
    {
        var request = ValidRequest();

        var result = await _validator.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
    }
}
