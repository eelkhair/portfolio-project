using CompanyApi.Application.Commands.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace CompanyApi.Features.Companies.Create;

public class CreateCompanyEndpoint(ICompanyCommandService service, ILogger<CreateCompanyEndpoint> logger)
    : Endpoint<CreateCompanyRequest, CompanyResponse>
{
    public override void Configure()
    {
        Post("/companies");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating Company: {Name}", request.Name);
        var company = await service.CreateAsync(request, User, ct);
        await Send.OkAsync(company, cancellation:ct);
    }
    
}