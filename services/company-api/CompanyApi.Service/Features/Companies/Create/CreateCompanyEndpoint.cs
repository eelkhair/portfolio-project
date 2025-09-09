using CompanyApi.Application.Commands.Interfaces;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace CompanyApi.Features.Companies.Create;

public class CreateCompanyEndpoint(ICompanyCommandService service)
    : Endpoint<CreateCompanyRequest, CompanyResponse>
{
    public override void Configure()
    {
        Post("/companies");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var company = await service.CreateAsync(request, User, ct);
        await SendAsync(company, cancellation:ct);
    }
    
}