using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands;

public class CompanyCommandService(DaprClient client, UserContextService accessor) : ICompanyCommandService
{
    public async Task<ApiResponse<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var message = client.CreateInvokeMethodRequest(HttpMethod.Post, "company-api", "companies");
        message.Headers.Add("Authorization", accessor.GetHeader("Authorization"));
        message.Content=  JsonContent.Create(request);
        return await DaprExtensions.Process(() =>
            client.InvokeMethodAsync<CompanyResponse>(message,cancellationToken: ct));
    }

}