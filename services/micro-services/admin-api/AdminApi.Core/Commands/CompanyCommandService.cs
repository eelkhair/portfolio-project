using AdminAPI.Contracts.Services;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using UserAPI.Contracts.Models.Events;

namespace AdminApi.Application.Commands;

public class CompanyCommandService(DaprClient client, UserContextService accessor, IMessageSender sender, ILogger<CompanyCommandService> logger) : ICompanyCommandService
{
    public async Task<ApiResponse<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        logger.LogInformation("Calling company-api to create company: {CompanyName}", request.Name);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "company-api", "api/companies");
        message.Headers.Add("Authorization", accessor.GetHeader("Authorization"));
        message.Content=  JsonContent.Create(request);
        var company = await DaprExtensions.Process(() =>
            client.InvokeMethodAsync<CompanyResponse>(message,cancellationToken: ct));

        if (company.Success && company.Data is { } data)
        {
            await sender.SendEventAsync(PubSubNames.RabbitMq, "company.created",
                request.UserId ?? accessor.GetCurrentUser() ?? "unknown",
                new ProvisionUserEvent
                {
                    CompanyName = data.Name,
                    FirstName = request.AdminFirstName,
                    LastName = request.AdminLastName,
                    Email = request.AdminEmail,
                    WebSite = request.CompanyWebsite,
                    CompanyUId = data.UId,
                    CompanyEmail = request.CompanyEmail,
                    UId = request.AdminUserId,
                    UserCompanyUId = request.UserCompanyId
                }, ct);
        }

        return company;
    }

    public async Task<ApiResponse<CompanyResponse>> UpdateAsync(Guid companyUId, UpdateCompanyRequest request, CancellationToken ct)
    {
        logger.LogInformation("Calling company-api to update company: {CompanyUId}", companyUId);
        var message = client.CreateInvokeMethodRequest(HttpMethod.Put, "company-api", $"api/companies/{companyUId}");
        message.Headers.Add("Authorization", accessor.GetHeader("Authorization"));
        message.Content = JsonContent.Create(request);
        return await DaprExtensions.Process(() =>
            client.InvokeMethodAsync<CompanyResponse>(message, cancellationToken: ct));
    }
}