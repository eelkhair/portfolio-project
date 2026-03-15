using System.Diagnostics;
using AdminAPI.Contracts.Services;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Companies.Create;

public class CreateCompanyEndpoint(ICompanyCommandService service,
    ILogger<CreateCompanyEndpoint> logger)
    : Endpoint<CreateCompanyRequest, ApiResponse<CompanyResponse>>
{
    public override void Configure()
    {
        Post("/companies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating company {@Request}", request);
        Activity.Current?.SetTag("input.companyId", request.CompanyId);
        Activity.Current?.SetTag("input.admin.user.id", request.AdminUserId);
        Activity.Current?.SetTag("input.user.companyId", request.UserCompanyId);
        var company = await service.CreateAsync(request, ct);
        await Send.OkAsync(company, ct);
    }
}
