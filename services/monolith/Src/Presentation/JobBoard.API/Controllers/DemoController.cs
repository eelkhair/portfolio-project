using JobBoard.API.Helpers;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Application.Actions.Companies.Demo;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.API.Controllers;

/// <summary>
/// Anonymous demo endpoints — used by the landing-page chatbot to let visitors
/// experience real company creation, claim it as their own after sign-up, and
/// look up their demo company. Demo companies are auto-deleted after the TTL.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/demo")]
[Produces("application/json")]
public class DemoController : ControllerBase
{
    private const int DemoTtlMinutes = 60;
    private const string SyntheticDemoEmailDomain = "demo.elkhair.tech";

    /// <summary>
    /// Create a demo company. Backed by the same handler the admin flow uses,
    /// but with IsDemo=true and a 1h TTL. Synthesizes admin identity.
    /// </summary>
    [HttpPost("companies")]
    [ProducesResponseType(typeof(DemoCompanyCreatedResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDemoCompany(
        [FromBody] CreateDemoCompanyRequest request,
        [FromServices] IDemoClaimTokenService claimTokenService,
        [FromServices] IJobBoardQueryDbContext queryContext,
        CancellationToken cancellationToken)
    {
        var adminUid = Guid.NewGuid();
        var syntheticEmail = $"demo-{adminUid:N}@{SyntheticDemoEmailDomain}";
        var expiresAt = DateTime.UtcNow.AddMinutes(DemoTtlMinutes);

        // Resolve industry: explicit UID wins; otherwise fuzzy-match on the hint;
        // otherwise pick the first available industry as a safe default.
        var industryUId = await ResolveIndustryUId(queryContext, request, cancellationToken);

        var command = new Application.Actions.Companies.Demo.CreateDemoCompanyCommand
        {
            Name = request.Name,
            CompanyEmail = $"contact-{adminUid:N}@{SyntheticDemoEmailDomain}",
            IndustryUId = industryUId,
            AdminFirstName = request.AdminFirstName ?? "Demo",
            AdminLastName = request.AdminLastName ?? "Visitor",
            AdminEmail = syntheticEmail,
            DemoExpiresAt = expiresAt,
            UserId = "demo-anonymous"
        };

        var handlerType = typeof(IHandler<Application.Actions.Companies.Demo.CreateDemoCompanyCommand, CompanyDto>);
        var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
        var company = await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);

        var claimToken = claimTokenService.Issue(company.Id, expiresAt);
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? string.Empty;

        var response = new DemoCompanyCreatedResponse
        {
            Company = company,
            DemoExpiresAt = expiresAt,
            ClaimToken = claimToken,
            TraceId = traceId
        };

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Success(response));
    }

    /// <summary>
    /// Anonymous industry catalog for the demo chat flow. The same data is
    /// available through /odata/industries to authenticated callers; this
    /// projection lets the landing chatbot surface options without auth.
    /// </summary>
    [HttpGet("industries")]
    [ProducesResponseType(typeof(List<IndustryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDemoIndustries(
        [FromServices] IJobBoardQueryDbContext queryContext,
        CancellationToken cancellationToken)
    {
        var industries = await queryContext.Industries
            .Where(i => Microsoft.EntityFrameworkCore.EF.Property<DateTime>(i, "PeriodEnd") == DateTime.MaxValue)
            .Select(i => new IndustryDto { Id = i.Id, Name = i.Name })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse.Success(industries));
    }

    /// <summary>
    /// Look up a demo company by id. Public-by-id only — no list endpoint.
    /// </summary>
    [HttpGet("companies/{uid:guid}")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDemoCompany(Guid uid, CancellationToken cancellationToken)
    {
        var query = new GetDemoCompanyByIdQuery { CompanyUId = uid };
        var handlerType = typeof(IHandler<GetDemoCompanyByIdQuery, CompanyDto?>);
        var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
        var company = await ((dynamic)handler).HandleAsync((dynamic)query, cancellationToken);

        if (company is null)
            return NotFound(ApiResponse.Fail<object>("Demo company not found or already expired", System.Net.HttpStatusCode.NotFound));

        return Ok(ApiResponse.Success((CompanyDto)company));
    }

    /// <summary>
    /// Internal hard-delete called by connector-api at the end of the
    /// DemoCompanyExpiredSaga, after every microservice + Keycloak group has been torn
    /// down. Authorized via the InternalOrJwt policy used elsewhere for connector-only
    /// endpoints. Idempotent — returns 404 if the row is already gone.
    /// </summary>
    [HttpDelete("companies/{uid:guid}/internal")]
    [Authorize(Policy = AuthorizationPolicies.InternalOrJwt)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InternalDeleteDemoCompany(Guid uid, CancellationToken cancellationToken)
    {
        var command = new DeleteDemoCompanyCommand
        {
            CompanyUId = uid,
            UserId = "demo-cleanup"
        };

        try
        {
            var handlerType = typeof(IHandler<DeleteDemoCompanyCommand, Unit>);
            var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
            await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail<object>(ex.Message, System.Net.HttpStatusCode.Conflict));
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("NotFound", StringComparison.Ordinal))
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Claim a demo company by exchanging the claim token + a real Keycloak user id
    /// for full CompanyAdmin ownership. Strips the IsDemo flag and dispatches a
    /// DemoCompanyClaimedV1Event so connector-api / user-api can repoint the admin.
    /// </summary>
    [HttpPost("companies/{uid:guid}/claim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimDemoCompany(
        Guid uid,
        [FromBody] ClaimDemoCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ClaimDemoCompanyCommand
        {
            CompanyUId = uid,
            ClaimToken = request.ClaimToken,
            NewAdminEmail = request.Email,
            NewAdminFirstName = request.FirstName,
            NewAdminLastName = request.LastName,
            NewKeycloakUserId = request.KeycloakUserId,
            UserId = request.KeycloakUserId
        };

        try
        {
            var handlerType = typeof(IHandler<ClaimDemoCompanyCommand, Unit>);
            var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
            await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);
            return Ok(ApiResponse.Success(new { uid }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail<object>(ex.Message, System.Net.HttpStatusCode.Unauthorized));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail<object>(ex.Message, System.Net.HttpStatusCode.Conflict));
        }
    }

    private static async Task<Guid> ResolveIndustryUId(
        IJobBoardQueryDbContext queryContext,
        CreateDemoCompanyRequest request,
        CancellationToken ct)
    {
        if (request.IndustryUId.HasValue && request.IndustryUId.Value != Guid.Empty)
            return request.IndustryUId.Value;

        var hint = (request.IndustryHint ?? string.Empty).Trim();
        var industries = queryContext.Industries
            .Where(i => EF.Property<DateTime>(i, "PeriodEnd") == DateTime.MaxValue);

        if (!string.IsNullOrEmpty(hint))
        {
            var match = await industries
                .Where(i => i.Name != null && i.Name.Contains(hint))
                .Select(i => i.Id)
                .FirstOrDefaultAsync(ct);
            if (match != Guid.Empty) return match;
        }

        // Fallback: first available industry. Demo companies don't care about the
        // exact value — this just satisfies the FK / domain validation.
        return await industries.Select(i => i.Id).FirstOrDefaultAsync(ct);
    }
}

public sealed class CreateDemoCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Optional. Caller may provide an explicit industry UID OR a free-text hint
    /// (e.g. "tech", "finance"); the server resolves the hint to a real industry.
    /// </summary>
    public Guid? IndustryUId { get; set; }
    public string? IndustryHint { get; set; }
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
}

public sealed class ClaimDemoCompanyRequest
{
    public string ClaimToken { get; set; } = string.Empty;
    public string KeycloakUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public sealed class DemoCompanyCreatedResponse
{
    public CompanyDto Company { get; set; } = null!;
    public DateTime DemoExpiresAt { get; set; }
    public string ClaimToken { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
}
