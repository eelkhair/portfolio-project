using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using UserAPI.Contracts.Models.Events;

namespace AdminApi.Features.Companies.Create;

public class CreateCompanyEndpoint(ICompanyCommandService service, IMessageSender sender)
    : Endpoint<CreateCompanyRequest, ApiResponse<CompanyResponse>>
{
    public override void Configure()
    {
        Post("/companies");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var company = await service.CreateAsync(request, ct);

        if (company.Success)
        {
            await sender.SendEventAsync(PubSubNames.RabbitMq, "provision.user", User.GetUserId(), new
                ProvisionUserEvent
            {
                CompanyName = company.Data?.Name,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                Email= request.AdminEmail,
                WebSite = request.CompanyWebsite,
                CompanyUId = company.Data?.UId,
                CompanyEmail = request.CompanyEmail
                
            }, ct);
        }
        await SendAsync(company, (int) company.StatusCode, ct);
    }
}