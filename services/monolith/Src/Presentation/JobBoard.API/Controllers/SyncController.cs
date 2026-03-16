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
    private void SetSyncUserContext(string userId)
    {
        accessor.UserId = userId;
        // Override identity fields so UserSyncService creates a distinct user
        // (avoids unique email conflicts with JWT-authenticated users)
        accessor.Email = $"{userId}@sync.internal";
        accessor.FirstName = "Sync";
        accessor.LastName = "Service";
    }

    [HttpPost("drafts")]
    public async Task<IActionResult> SyncDraft([FromBody] SyncDraftRequest request, CancellationToken ct)
    {
        SetSyncUserContext(request.UserId);

        return await ExecuteCommandAsync(new SyncDraftSaveCommand
        {
            DraftId = request.DraftId,
            CompanyId = request.CompanyId,
            ContentJson = request.ContentJson
        }, Ok);
    }

    [HttpDelete("drafts/{draftId:guid}")]
    public async Task<IActionResult> DeleteDraft(Guid draftId, [FromQuery] Guid companyId,
        [FromHeader(Name = "x-user-id")] string? userId, CancellationToken ct)
    {
        SetSyncUserContext(userId ?? "system");

        return await ExecuteCommandAsync(new SyncDraftDeleteCommand
        {
            DraftId = draftId,
            CompanyId = companyId
        }, Ok);
    }

    [HttpPost("companies")]
    public async Task<IActionResult> SyncCompanyCreate([FromBody] SyncCompanyCreateRequest request, CancellationToken ct)
    {
        SetSyncUserContext(request.UserId);

        return await ExecuteCommandAsync(new SyncCompanyCreateCommand
        {
            CompanyId = request.CompanyId,
            Name = request.Name,
            CompanyEmail = request.CompanyEmail,
            CompanyWebsite = request.CompanyWebsite,
            IndustryUId = request.IndustryUId,
            AdminFirstName = request.AdminFirstName,
            AdminLastName = request.AdminLastName,
            AdminEmail = request.AdminEmail,
            AdminUId = request.AdminUId,
            UserCompanyUId = request.UserCompanyUId
        }, Ok);
    }

    [HttpPut("companies/{companyId:guid}")]
    public async Task<IActionResult> SyncCompanyUpdate(Guid companyId, [FromBody] SyncCompanyUpdateRequest request, CancellationToken ct)
    {
        SetSyncUserContext(request.UserId);

        return await ExecuteCommandAsync(new SyncCompanyUpdateCommand
        {
            CompanyId = companyId,
            Name = request.Name,
            CompanyEmail = request.CompanyEmail,
            CompanyWebsite = request.CompanyWebsite,
            Phone = request.Phone,
            Description = request.Description,
            About = request.About,
            EEO = request.EEO,
            Founded = request.Founded,
            Size = request.Size,
            Logo = request.Logo,
            IndustryUId = request.IndustryUId
        }, Ok);
    }

    [HttpPost("jobs")]
    public async Task<IActionResult> SyncJobCreate([FromBody] SyncJobCreateRequest request, CancellationToken ct)
    {
        SetSyncUserContext(request.UserId);

        return await ExecuteCommandAsync(new SyncJobCreateCommand
        {
            JobId = request.JobId,
            CompanyId = request.CompanyId,
            Title = request.Title,
            AboutRole = request.AboutRole,
            Location = request.Location,
            SalaryRange = request.SalaryRange,
            JobType = request.JobType,
            Responsibilities = request.Responsibilities,
            Qualifications = request.Qualifications
        }, Ok);
    }
}
