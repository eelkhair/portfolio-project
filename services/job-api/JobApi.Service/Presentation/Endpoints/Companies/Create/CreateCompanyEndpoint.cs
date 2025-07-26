using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using FastEndpoints;
using JobApi.Application.Commands.Companies;
using JobAPI.Contracts.Models.Companies.Requests;
using JobAPI.Contracts.Models.Companies.Responses;

namespace JobApi.Presentation.Endpoints.Companies.Create;

public class CreateCompanyEndpoint(IMediator mediator, IRequestFactory factory)
    : Endpoint<CreateCompanyRequest, CompanyResponse, CompanyMapper>
{
    public override void Configure()
    {
        Post("/companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var company = Map.ToEntity(request);
        var command = factory.Create<CreateCompanyCommand>(company);
        var response = await mediator.SendAsync(command, ct);
        await SendAsync(Map.FromEntity(response), cancellation:ct);
    }
    
}