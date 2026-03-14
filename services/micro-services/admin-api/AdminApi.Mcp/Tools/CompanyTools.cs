using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Models.Companies.Requests;
using AdminAPI.Contracts.Services;
using ModelContextProtocol.Server;

namespace AdminApi.Mcp.Tools;

[McpServerToolType]
public class CompanyTools(
    ICompanyCommandService commandService,
    ICompanyQueryService queryService)
{
    [McpServerTool(Name = "company_list"), Description("Returns a list of all companies in the system.")]
    public async Task<string> ListCompanies(CancellationToken ct)
    {
        var response = await queryService.ListAsync(ct);
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
        var request = new CreateCompanyRequest
        {
            Name = name,
            CompanyEmail = companyEmail,
            CompanyWebsite = companyWebsite,
            IndustryUId = industryUId,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
            AdminEmail = adminEmail
        };

        var response = await commandService.CreateAsync(request, ct);
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

        var response = await commandService.UpdateAsync(companyId, request, ct);
        return JsonSerializer.Serialize(response.Data);
    }
}
