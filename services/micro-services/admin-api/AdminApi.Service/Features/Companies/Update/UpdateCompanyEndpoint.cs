using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Companies.Update;

public class UpdateCompanyEndpoint(ICompanyCommandService service, ILogger<UpdateCompanyEndpoint> logger)
    : Endpoint<UpdateCompanyRequest, ApiResponse<CompanyResponse>>
{
    public override void Configure()
    {
        Put("/companies/{id}");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(UpdateCompanyRequest request, CancellationToken ct)
    {
        var companyUId = Route<Guid>("id");
        logger.LogInformation("Updating company {CompanyUId}", companyUId);
        var company = await service.UpdateAsync(companyUId, request, ct);
        await Send.OkAsync(company, ct);
    }
}
