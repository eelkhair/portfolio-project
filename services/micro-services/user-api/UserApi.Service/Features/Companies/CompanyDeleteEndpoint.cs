using System.Diagnostics;
using FastEndpoints;
using UserApi.Application.Commands.Interfaces;

namespace UserApi.Features.Companies;

public class CompanyDeleteEndpoint(
    ActivitySource activitySource,
    IKeycloakCommandService keycloak,
    ICompanyCommandService commandService,
    ILogger<CompanyDeleteEndpoint> logger)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("companies/{companyUId:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companyUId = Route<Guid>("companyUId");

        using var activity = activitySource.StartActivity("user-api.demo-company.delete");
        activity?.SetTag("company.uid", companyUId);

        logger.LogInformation("Tearing down user-api + Keycloak for demo company {CompanyUId}", companyUId);

        // Keycloak first — once the group is gone, the user-api projection is the only
        // thing referencing those synthetic users so we can drop them safely.
        await keycloak.TeardownCompanyAsync(companyUId, ct);
        await commandService.DeleteCompanyAsync(companyUId, "demo-cleanup", ct);

        await Send.NoContentAsync(ct);
    }
}
