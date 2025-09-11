using System.Net;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models;
using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Domain.Constants;

namespace AdminApi.Application.Commands;

public class CompanyCommandService(DaprClient client) : ICompanyCommandService
{
    public async Task<ApiResponse<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var response = await DaprExtensions.Process(() =>
            client.InvokeMethodAsync<CreateCompanyRequest, CompanyResponse>(
                HttpMethod.Post,
                "company-api",
                "companies",
                request,
                ct));
        
        await client.PublishEventAsync(PubSubNames.RabbitMq, "company.created", response.Data, ct);
        
        return response;
    }
}