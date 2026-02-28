using CompanyApi.Application.Commands.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace CompanyApi.Features.Companies.Update;

public class UpdateCompanyEndpoint(ICompanyCommandService service, ILogger<UpdateCompanyEndpoint> logger)
    : Endpoint<UpdateCompanyRequest, CompanyResponse>
{
    public override void Configure()
    {
        Put("/companies/{id}");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(UpdateCompanyRequest request, CancellationToken ct)
    {
        var companyUId = Route<Guid>("id");
        logger.LogInformation("Updating Company: {CompanyUId}", companyUId);
        var company = await service.UpdateAsync(companyUId, request, User, ct);
        await Send.OkAsync(company, cancellation: ct);
    }
}
