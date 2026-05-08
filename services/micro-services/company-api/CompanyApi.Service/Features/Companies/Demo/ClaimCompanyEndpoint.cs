using System.Diagnostics;
using CompanyAPI.Contracts.Models.Companies.Requests;
using FastEndpoints;

namespace CompanyApi.Features.Companies.Demo;

/// <summary>
/// Claim handler for company-api. The company-api Company entity does not track
/// admin user references — those live in user-api. This endpoint is currently a
/// no-op trace anchor so the connector saga can fan-out to all three microservices
/// uniformly. If we ever add an IsDemo flag on company-api's projection, clear it here.
/// </summary>
public class ClaimCompanyEndpoint(ILogger<ClaimCompanyEndpoint> logger)
    : Endpoint<DemoCompanyClaimedPayload>
{
    public override void Configure()
    {
        Post("/companies/{companyUId:guid}/claim");
        AllowAnonymous();
    }

    public override Task HandleAsync(DemoCompanyClaimedPayload payload, CancellationToken ct)
    {
        var companyUId = Route<Guid>("companyUId");
        Activity.Current?.SetTag("entity.type", "company");
        Activity.Current?.SetTag("operation", "claim");
        Activity.Current?.SetTag("company.uid", companyUId);
        Activity.Current?.SetTag("new.admin.email", payload.NewAdminEmail);

        logger.LogInformation(
            "Claim ack for company-api {CompanyUId} → {NewAdminEmail} (no-op: no admin reference on company-api projection)",
            companyUId, payload.NewAdminEmail);

        return Send.NoContentAsync(ct);
    }
}
