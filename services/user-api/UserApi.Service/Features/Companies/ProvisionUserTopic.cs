using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;

namespace UserApi.Features.Companies;

public class CompanyCreatedTopic : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/companies/create");
        Permissions("create:companies");
         Options(c => 
             c.WithTopic(PubSubNames.RabbitMq, "company.created"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendAsync(new(), cancellation: ct);
    }
    
}
