using Dapr.Client;
using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Requests;
using JobAPI.Contracts.Models.Companies.Responses;

namespace JobApi.Features.Companies.Create;

public class CreateCompanyEndpoint
    : Endpoint<CreateCompanyRequest, CompanyResponse>
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