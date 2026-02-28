using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Companies.Requests;

namespace JobApi.Features.Companies.Update;

public class UpdateCompanyEndpoint(ICompanyCommandService service, ILogger<UpdateCompanyEndpoint> logger)
    : Endpoint<UpdateCompanyRequest>
{
    public override void Configure()
    {
        Put("companies/{id}");
    }

    public override async Task HandleAsync(UpdateCompanyRequest request, CancellationToken ct)
    {
        var companyUId = Route<Guid>("id");
        logger.LogInformation("Updating Company: {CompanyUId}", companyUId);
        await service.UpdateCompanyAsync(companyUId, request, User, ct);
    }
}
