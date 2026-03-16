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

    [HttpPost("companies")]
    public async Task<IActionResult> SyncCompanyCreate([FromBody] SyncCompanyCreateRequest request, CancellationToken ct)
    {
        accessor.UserId = request.UserId;

        await ExecuteCommandAsync(new SyncCompanyCreateCommand
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

        return Ok();
    }

    [HttpPut("companies/{companyId:guid}")]
    public async Task<IActionResult> SyncCompanyUpdate(Guid companyId, [FromBody] SyncCompanyUpdateRequest request, CancellationToken ct)
    {
        accessor.UserId = request.UserId;

        await ExecuteCommandAsync(new SyncCompanyUpdateCommand
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

        return Ok();
    }

    [HttpPost("jobs")]
    public async Task<IActionResult> SyncJobCreate([FromBody] SyncJobCreateRequest request, CancellationToken ct)
    {
        accessor.UserId = request.UserId;

        await ExecuteCommandAsync(new SyncJobCreateCommand
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

        return Ok();
    }
}
