using System.Diagnostics;
using FastEndpoints;
using JobApi.Application.Interfaces;

namespace JobApi.Features.Companies.Demo;

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

        logger.LogInformation("Deleting job-api company {CompanyUId} for demo cleanup", companyUId);
        await service.DeleteCompanyAsync(companyUId, User, ct);
        await Send.NoContentAsync(ct);
    }
}
