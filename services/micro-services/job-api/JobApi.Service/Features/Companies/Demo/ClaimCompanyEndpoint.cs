using System.Diagnostics;
using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Requests;

namespace JobApi.Features.Companies.Demo;

/// <summary>
/// Claim handler for job-api. The job-api Company entity has no admin user reference —
/// jobs are owned by the company itself. This endpoint is currently a no-op trace
/// anchor so the connector saga can fan-out uniformly.
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
            "Claim ack for job-api {CompanyUId} → {NewAdminEmail} (no-op: no admin reference on job-api projection)",
            companyUId, payload.NewAdminEmail);

        return Send.NoContentAsync(ct);
    }
}
