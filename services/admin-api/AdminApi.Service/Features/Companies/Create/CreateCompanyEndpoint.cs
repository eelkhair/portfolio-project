using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Companies.Responses;
using FastEndpoints;

namespace AdminApi.Features.Companies.Create;

public class CreateCompanyEndpoint
    : Endpoint<CreateCompanyRequest, CompanyResponse>
{
    public override void Configure()
    {
        Post("/companies");
        Permissions("create:companies");
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
    
        await SendAsync(new CompanyResponse(), cancellation:ct);
    }
    
}