using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models;
using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Companies.Create;

public class CreateCompanyEndpoint(ICompanyCommandService service)
    : Endpoint<CreateCompanyRequest, ApiResponse<CompanyResponse>>
{
    public override void Configure()
    {
        Post("/companies");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        request.CreatedByUserId = User.GetUserId();
        var company = await service.CreateAsync(request, ct);
        await SendAsync(company, (int) company.StatusCode, ct);
        
    }
}