using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.IntegrationEvents.Draft;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Mappers;

public static class DraftMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Maps a DraftSavedV1Event into a SyncDraftPayload.
    /// Reconstructs the ContentJson from the event fields to match
    /// the monolith's draft JSON format (same as SaveDraftRequest/DraftResponse).
    /// </summary>
    public static SyncDraftPayload ToPayload(DraftSavedV1Event evt)
    {
        var content = new
        {
            Id = evt.UId.ToString(),
            evt.Title,
            evt.AboutRole,
            evt.Location,
            evt.JobType,
            evt.SalaryRange,
            evt.Notes,
            evt.Responsibilities,
            evt.Qualifications
        };

        return new SyncDraftPayload
        {
            DraftId = evt.UId,
            CompanyId = evt.CompanyUId,
            ContentJson = JsonSerializer.Serialize(content, JsonOpts)
        };
    }
}
