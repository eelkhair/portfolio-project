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
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateCompanyRequest request, CancellationToken ct)
    {
        var companyUId = Route<Guid>("id");
        var isForwardSync = HttpContext.Request.Headers["X-Sync-Source"].FirstOrDefault() == "forward";
        logger.LogInformation("Updating Company: {CompanyUId}, isForwardSync: {IsForwardSync}", companyUId, isForwardSync);
        var company = await service.UpdateAsync(companyUId, request, User, ct, publishEvent: !isForwardSync);
        await Send.OkAsync(company, cancellation: ct);
    }
}
