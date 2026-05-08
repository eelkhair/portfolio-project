using System.Diagnostics;
using CompanyApi.Application.Commands.Interfaces;
using FastEndpoints;

namespace CompanyApi.Features.Companies.Demo;

public class DeleteCompanyEndpoint(ICompanyCommandService service, ILogger<DeleteCompanyEndpoint> logger)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/companies/{companyUId:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var companyUId = Route<Guid>("companyUId");
        Activity.Current?.SetTag("entity.type", "company");
        Activity.Current?.SetTag("operation", "delete");
        Activity.Current?.SetTag("company.uid", companyUId);

        logger.LogInformation("Deleting company-api row for demo cleanup {CompanyUId}", companyUId);
        await service.DeleteAsync(companyUId, User, ct);
        await Send.NoContentAsync(ct);
    }
}
