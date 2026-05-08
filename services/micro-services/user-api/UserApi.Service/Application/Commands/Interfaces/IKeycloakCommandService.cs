using UserApi.Infrastructure.Keycloak;
using UserAPI.Contracts.Models.Events;

namespace UserApi.Application.Commands.Interfaces;

public interface IKeycloakCommandService
{
    Task<(KeycloakUser User, KeycloakGroup Group)> ProvisionUserAsync(ProvisionUserEvent user, CancellationToken ct);

    /// <summary>Tear down the Companies/{uid} group and all members (synthetic demo admin etc).</summary>
    Task TeardownCompanyAsync(Guid companyUId, CancellationToken ct);

    /// <summary>
    /// Swap the synthetic demo admin user for a real signed-up user on the
    /// Companies/{uid}/CompanyAdmins group: create or find the new user, add them
    /// to the group, then remove + delete the synthetic admin.
    /// </summary>
    Task SwapDemoAdminAsync(
        Guid companyUId,
        string newAdminEmail,
        string newAdminFirstName,
        string newAdminLastName,
        CancellationToken ct);
}
