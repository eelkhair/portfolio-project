using System.Diagnostics;
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

public class CreateCompanyEndpoint(ICompanyCommandService service, 
    IMessageSender sender,
    ILogger<CreateCompanyEndpoint> logger)
    : Endpoint<CreateCompanyRequest, ApiResponse<CompanyResponse>>
{
    public override void Configure()
    {
        Post("/companies");
        Permissions("write:companies");
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating company {@Request}", request);
        Activity.Current?.SetTag("input.companyId", request.CompanyId);
        Activity.Current?.SetTag("input.admin.user.id", request.AdminUserId);
        Activity.Current?.SetTag("input.user.companyId", request.UserCompanyId);
        var company = await service.CreateAsync(request, ct);

        if (company.Success)
        {
            await sender.SendEventAsync(PubSubNames.RabbitMq, "company.created", request.UserId ??
            User.GetUserId(), new
                ProvisionUserEvent
            {
                CompanyName = company.Data?.Name!,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                Email= request.AdminEmail,
                WebSite = request.CompanyWebsite,
                CompanyUId = company.Data?.UId ?? Guid.Empty,
                CompanyEmail = request.CompanyEmail, 
                UId = request.AdminUserId,
                UserCompanyUId = request.UserCompanyId
                
            }, ct);
        }
        await Send.OkAsync(company, ct);
    }
}