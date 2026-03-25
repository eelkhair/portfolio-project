using AdminApi.Features.Jobs.DraftUpsert;
using AdminAPI.Contracts.Models.Jobs;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Shouldly;

namespace AdminApi.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public class UpsertDraftValidatorTests
{
    private readonly UpsertDraftValidator _validator = new();

    private static JobDraftRequest ValidRequest() => new()
    {
        Title = "Senior Engineer",
        AboutRole = "Lead backend development",
        Metadata = new JobGenMetadata
        {
            RoleLevel = RoleLevel.Senior,
            Tone = Tone.Neutral
        }
    };

    // ── Metadata.RoleLevel ──

    [Fact]
    public async Task Metadata_RoleLevel_WhenValidEnum_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.Metadata.RoleLevel = RoleLevel.Staff;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Metadata.RoleLevel");
    }

    [Fact]
    public async Task Metadata_RoleLevel_WhenInvalidEnum_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Metadata.RoleLevel = (RoleLevel)99;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "Metadata.RoleLevel");
    }

    // ── Metadata.Tone ──

    [Fact]
    public async Task Metadata_Tone_WhenValidEnum_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.Metadata.Tone = Tone.Friendly;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Metadata.Tone");
    }

    [Fact]
    public async Task Metadata_Tone_WhenInvalidEnum_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Metadata.Tone = (Tone)99;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldContain(e => e.PropertyName == "Metadata.Tone");
    }

    // ── Metadata null ──

    [Fact]
    public async Task Metadata_WhenNull_ShouldNotValidateEnums()
    {
        var request = ValidRequest();
        request.Metadata = null!;

        var result = await _validator.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
    }

    // ── All valid ──

    [Fact]
    public async Task ValidRequest_ShouldPassAllRules()
    {
        var request = ValidRequest();

        var result = await _validator.ValidateAsync(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(RoleLevel.Junior)]
    [InlineData(RoleLevel.Mid)]
    [InlineData(RoleLevel.Senior)]
    [InlineData(RoleLevel.Staff)]
    [InlineData(RoleLevel.Principal)]
    public async Task Metadata_RoleLevel_AllValidValues_ShouldPass(RoleLevel level)
    {
        var request = ValidRequest();
        request.Metadata.RoleLevel = level;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Metadata.RoleLevel");
    }

    [Theory]
    [InlineData(Tone.Neutral)]
    [InlineData(Tone.Concise)]
    [InlineData(Tone.Friendly)]
    public async Task Metadata_Tone_AllValidValues_ShouldPass(Tone tone)
    {
        var request = ValidRequest();
        request.Metadata.Tone = tone;

        var result = await _validator.ValidateAsync(request);

        result.Errors.ShouldNotContain(e => e.PropertyName == "Metadata.Tone");
    }
}
