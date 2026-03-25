using ReverseConnectorAPI.Models;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Models;

[Trait("Category", "Unit")]
public class SyncDraftPayloadTests
{
    [Fact]
    public void ContentJson_DefaultsToEmptyJsonObject()
    {
        var payload = new SyncDraftPayload();

        payload.ContentJson.ShouldBe("{}");
    }

    [Fact]
    public void DraftId_DefaultsToEmptyGuid()
    {
        var payload = new SyncDraftPayload();

        payload.DraftId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void CompanyId_DefaultsToEmptyGuid()
    {
        var payload = new SyncDraftPayload();

        payload.CompanyId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var draftId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var payload = new SyncDraftPayload
        {
            DraftId = draftId,
            CompanyId = companyId,
            ContentJson = "{\"title\":\"Test\"}"
        };

        payload.DraftId.ShouldBe(draftId);
        payload.CompanyId.ShouldBe(companyId);
        payload.ContentJson.ShouldBe("{\"title\":\"Test\"}");
    }
}
