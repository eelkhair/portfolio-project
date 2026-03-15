using JobBoard.Application.Actions.Sync;
using JobBoard.Domain;
using JobBoard.Mcp.Common;
using JobBoard.Monolith.Contracts.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Reverse-sync endpoints called by reverse-connector-api.
/// Handlers do NOT publish outbox events to prevent infinite sync loops.
/// </summary>
[Route("api/sync")]
[Authorize(Policy = AuthorizationPolicies.InternalOrJwt)]
public class SyncController(IUserAccessor accessor) : BaseApiController
{
    [HttpPost("drafts")]
    public async Task<IActionResult> SyncDraft([FromBody] SyncDraftRequest request, CancellationToken ct)
    {
        accessor.UserId = request.UserId;

        await ExecuteCommandAsync(new SyncDraftSaveCommand
        {
            DraftId = request.DraftId,
            CompanyId = request.CompanyId,
            ContentJson = request.ContentJson
        }, Ok);

        return Ok();
    }

    [HttpDelete("drafts/{draftId:guid}")]
    public async Task<IActionResult> DeleteDraft(Guid draftId, [FromQuery] Guid companyId,
        [FromHeader(Name = "x-user-id")] string? userId, CancellationToken ct)
    {
        accessor.UserId = userId ?? "system";

        await ExecuteCommandAsync(new SyncDraftDeleteCommand
        {
            DraftId = draftId,
            CompanyId = companyId
        }, Ok);

        return Ok();
    }
}
