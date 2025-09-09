using System.Security.Claims;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;

namespace AdminApi.Application.Commands;

public class CompanyCommandService(DaprClient client): ICompanyCommandService
{
    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        return await client.InvokeMethodAsync<CreateCompanyRequest, CompanyResponse>(HttpMethod.Post, "company-api", "companies", request, ct);
    }
}