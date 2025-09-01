using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Requests;
using JobAPI.Contracts.Models.Companies.Responses;

namespace JobApi.Presentation.Endpoints.Companies.Create;

public class CreateCompanyEndpoint
    : Endpoint<CreateCompanyRequest, CompanyResponse, CompanyMapper>
{
    public override void Configure()
    {
        Post("/companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        await SendAsync(new CompanyResponse(), cancellation:ct);
    }
    
}