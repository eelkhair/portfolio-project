using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Models.Companies.Requests;
using JobBoard.AI.Application.Interfaces.Clients;
using ModelContextProtocol.Server;

namespace JobBoard.AI.MCP.Integration.Tools.Admin;

[McpServerToolType]
public class CompanyTools(
    IAdminApiClient adminClient,
    IMonolithApiClient monolithClient,
    IConfiguration configuration)
{
    private bool IsMonolith => configuration.GetValue<bool>("FeatureFlags:Monolith");

    [McpServerTool(Name = "company_list"), Description("Returns a list of all companies in the system.")]
    public async Task<string> ListCompanies(CancellationToken ct)
    {
        if (IsMonolith)
        {
            var result = await monolithClient.ListCompaniesAsync(ct);
            return JsonSerializer.Serialize(result.Value);
        }

        var response = await adminClient.ListCompaniesAsync(ct);
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "create_company"), Description("Creates a company with an admin user.")]
    public async Task<string> CreateCompany(
        [Description("Company name (required)")] string name,
        [Description("Company contact email (required)")] string companyEmail,
        [Description("Industry ID from industry_list (required)")] Guid industryUId,
        [Description("Admin user's first name (required)")] string adminFirstName,
        [Description("Admin user's last name (required)")] string adminLastName,
        [Description("Admin user's email address (required)")] string adminEmail,
        [Description("Company website URL (optional)")] string? companyWebsite = null,
        CancellationToken ct = default)
    {
        var cmd = new CreateCompanyRequest
        {
            Name = name,
            CompanyEmail = companyEmail,
            CompanyWebsite = companyWebsite,
            IndustryUId = industryUId,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
            AdminEmail = adminEmail
        };

        if (IsMonolith)
        {
            var monolithCmd = new CreateCompanyCommand
            {
                Name = name,
                CompanyEmail = companyEmail,
                CompanyWebsite = companyWebsite,
                IndustryUId = industryUId,
                AdminFirstName = adminFirstName,
                AdminLastName = adminLastName,
                AdminEmail = adminEmail
            };
            var result = await monolithClient.CreateCompanyAsync(monolithCmd, ct);
            return JsonSerializer.Serialize(result);
        }

        var response = await adminClient.CreateCompanyAsync(cmd, ct);
        return JsonSerializer.Serialize(response.Data);
    }

    [McpServerTool(Name = "update_company"),
     Description(
         "Updates an existing company. This is a full replacement — you MUST provide ALL fields, not just the ones being changed. " +
         "ALWAYS call company_list first to get the current values, then include every field (companyId, name, companyEmail, industryUId are required). " +
         "Omitted fields will be set to null/empty.")]
    public async Task<string> UpdateCompany(
        [Description("The company's unique identifier (required)")] Guid companyId,
        [Description("Company name (required)")] string name,
        [Description("Company contact email (required)")] string companyEmail,
        [Description("Industry ID from industry_list (required)")] Guid industryUId,
        [Description("Company website URL")] string? companyWebsite = null,
        [Description("Company phone number")] string? phone = null,
        [Description("Short company description")] string? description = null,
        [Description("Detailed about section for the company")] string? about = null,
        [Description("Equal Employment Opportunity statement")] string? eeo = null,
        [Description("Date the company was founded")] DateTime? founded = null,
        [Description("Company size (e.g. '50-100', '1000+')")] string? size = null,
        [Description("Company logo URL")] string? logo = null,
        CancellationToken ct = default)
    {
        if (IsMonolith)
        {
            var monolithCmd = new UpdateCompanyCommand
            {
                CompanyId = companyId,
                Name = name,
                CompanyEmail = companyEmail,
                CompanyWebsite = companyWebsite,
                Phone = phone,
                Description = description,
                About = about,
                EEO = eeo,
                Founded = founded,
                Size = size,
                Logo = logo,
                IndustryUId = industryUId
            };
            var result = await monolithClient.UpdateCompanyAsync(companyId, monolithCmd, ct);
            return JsonSerializer.Serialize(result);
        }

        var request = new UpdateCompanyRequest
        {
            Name = name,
            CompanyEmail = companyEmail,
            CompanyWebsite = companyWebsite,
            Phone = phone,
            Description = description,
            About = about,
            EEO = eeo,
            Founded = founded,
            Size = size,
            Logo = logo,
            IndustryUId = industryUId
        };
        var response = await adminClient.UpdateCompanyAsync(companyId, request, ct);
        return JsonSerializer.Serialize(response.Data);
    }
}
