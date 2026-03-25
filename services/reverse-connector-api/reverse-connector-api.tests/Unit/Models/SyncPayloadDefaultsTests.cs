using ReverseConnectorAPI.Models;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Models;

[Trait("Category", "Unit")]
public class SyncPayloadDefaultsTests
{
    [Fact]
    public void SyncDraftPayload_ContentJson_DefaultsToEmptyObject()
    {
        var payload = new SyncDraftPayload();

        payload.ContentJson.ShouldBe("{}");
        payload.DraftId.ShouldBe(Guid.Empty);
        payload.CompanyId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void SyncCompanyCreatePayload_Defaults_AreEmpty()
    {
        var payload = new SyncCompanyCreatePayload();

        payload.Name.ShouldBe(string.Empty);
        payload.CompanyEmail.ShouldBe(string.Empty);
        payload.CompanyWebsite.ShouldBeNull();
        payload.AdminFirstName.ShouldBe(string.Empty);
        payload.AdminLastName.ShouldBe(string.Empty);
        payload.AdminEmail.ShouldBe(string.Empty);
        payload.AdminUId.ShouldBeNull();
        payload.UserCompanyUId.ShouldBeNull();
    }

    [Fact]
    public void SyncCompanyUpdatePayload_Defaults_AreEmpty()
    {
        var payload = new SyncCompanyUpdatePayload();

        payload.Name.ShouldBe(string.Empty);
        payload.CompanyEmail.ShouldBe(string.Empty);
        payload.CompanyWebsite.ShouldBeNull();
        payload.Phone.ShouldBeNull();
        payload.Description.ShouldBeNull();
        payload.About.ShouldBeNull();
        payload.EEO.ShouldBeNull();
        payload.Founded.ShouldBeNull();
        payload.Size.ShouldBeNull();
        payload.Logo.ShouldBeNull();
    }

    [Fact]
    public void SyncJobCreatePayload_Defaults_AreEmpty()
    {
        var payload = new SyncJobCreatePayload();

        payload.Title.ShouldBe(string.Empty);
        payload.AboutRole.ShouldBe(string.Empty);
        payload.Location.ShouldBe(string.Empty);
        payload.SalaryRange.ShouldBeNull();
        payload.JobType.ShouldBe(string.Empty);
        payload.Responsibilities.ShouldBeEmpty();
        payload.Qualifications.ShouldBeEmpty();
    }

    [Fact]
    public void SyncJobCreatePayload_Collections_AreMutable()
    {
        var payload = new SyncJobCreatePayload();
        payload.Responsibilities.Add("Test");
        payload.Qualifications.Add("Qual");

        payload.Responsibilities.Count.ShouldBe(1);
        payload.Qualifications.Count.ShouldBe(1);
    }
}
