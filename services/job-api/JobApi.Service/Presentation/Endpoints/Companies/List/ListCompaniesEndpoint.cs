using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using FastEndpoints;
using JobApi.Application.Queries.Companies;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Presentation.Endpoints.Companies.Create;
namespace JobApi.Presentation.Endpoints.Companies.List;

public class ListCompaniesEndpoint(IMediator mediator, IRequestFactory factory) : EndpointWithoutRequest<List<CompanyResponse>, CompanyMapper>
{
    public override void Configure()
    {
        Get("/companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = factory.Create<ListCompaniesQuery>();
        var companies = await mediator.SendAsync(query, ct);
        await SendAsync(companies.Select(Map.FromEntity).ToList(), cancellation: ct);
    }
}
