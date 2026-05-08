using System.Diagnostics;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;
using UserAPI.Contracts.Models.Requests;

namespace UserApi.Features.Companies;

public class CompanyClaimEndpoint(
    ActivitySource activitySource,
    IKeycloakCommandService keycloak,
    ICompanyCommandService commandService,
    ILogger<CompanyClaimEndpoint> logger)
    : Endpoint<DemoCompanyClaimedPayload>
{
    public override void Configure()
    {
        Post("companies/{companyUId:guid}/claim");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DemoCompanyClaimedPayload payload, CancellationToken ct)
    {
        var companyUId = Route<Guid>("companyUId");

        using var activity = activitySource.StartActivity("user-api.demo-company.claim");
        activity?.SetTag("company.uid", companyUId);
        activity?.SetTag("new.admin.email", payload.NewAdminEmail);

        logger.LogInformation(
            "Claiming demo company {CompanyUId} → {NewAdminEmail}",
            companyUId, payload.NewAdminEmail);

        // Keycloak first — adds the real user to CompanyAdmins, then removes/deletes the
        // synthetic admin. user-api persistence then mirrors the swap.
        await keycloak.SwapDemoAdminAsync(
            companyUId,
            payload.NewAdminEmail,
            payload.NewAdminFirstName,
            payload.NewAdminLastName,
            ct);

        await commandService.RepointAdminAsync(
            companyUId,
            payload.NewAdminEmail,
            payload.NewAdminFirstName,
            payload.NewAdminLastName,
            payload.NewAdminUserUId,
            "demo-claim",
            ct);

        await Send.NoContentAsync(ct);
    }
}
