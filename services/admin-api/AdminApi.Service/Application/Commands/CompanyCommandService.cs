using System.Net;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models;
using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;

namespace AdminApi.Application.Commands;

public class CompanyCommandService(DaprClient client) : ICompanyCommandService
{
    public async Task<ApiResponse<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        return await Process(() =>
            client.InvokeMethodAsync<CreateCompanyRequest, CompanyResponse>(
                HttpMethod.Post,
                "company-api",
                "companies",
                request,
                ct));
    }

    private async Task<ApiResponse<T>> Process<T>(Func<Task<T>> func)
    {
        try
        {
            var response = await func().ConfigureAwait(false);
            return new ApiResponse<T>()
            {
                StatusCode = HttpStatusCode.OK,
                Data = response,
                Success = true
            };
        }
        catch (InvocationException e)
        {
            var error = await e.Response.Content.ReadFromJsonAsync<ApiError>();   
            return new ApiResponse<T>()
            {
                StatusCode = e.Response.StatusCode,
                Exceptions = error,
                Success = false
            };
        }
        
    }
}