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
    [McpServerTool(Name = "company_list"), Description("Returns a list of all companies (id, name, email).")]
    public async Task<string> ListCompanies(CancellationToken ct)
    {
        var response = await queryService.ListAsync(ct);
        var slim = response.Data?.Select(c => new { Id = c.UId, c.Name, c.Email }) ?? [];
        return JsonSerializer.Serialize(slim, Json.Opts);
    }

    [McpServerTool(Name = "company_detail"),
     Description("Returns full details for a single company by ID. Use after company_list to get description, about, website, phone, size, etc.")]
    public async Task<string> GetCompany(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        CancellationToken ct)
    {
        var (ok, id, err) = Json.ParseGuid(companyId, "companyId");
        if (!ok) return err!;

        var response = await queryService.ListAsync(ct);
        var company = response.Data?.FirstOrDefault(c => c.UId == id);

        if (company is null)
            return JsonSerializer.Serialize(new { error = "Company not found." }, Json.Opts);

        return JsonSerializer.Serialize(new
        {
            Id = company.UId,
            company.Name,
            company.Email,
            company.Description,
            company.About,
            company.Website,
            company.Phone,
            company.Founded,
            company.Size,
            company.EEO
        }, Json.Opts);
    }

    [McpServerTool(Name = "create_company"), Description("Creates a company with an admin user.")]
    public async Task<string> CreateCompany(
        [Description("Company name (required)")] string name,
        [Description("Company contact email (required)")] string companyEmail,
        [Description("Industry GUID from industry_list Id field (required)")] string industryUId,
        [Description("Admin user's first name (required)")] string adminFirstName,
        [Description("Admin user's last name (required)")] string adminLastName,
        [Description("Admin user's email address (required)")] string adminEmail,
        [Description("Company website URL (optional)")] string? companyWebsite = null,
        CancellationToken ct = default)
    {
        var (ok, indId, err) = Json.ParseGuid(industryUId, "industryUId");
        if (!ok) return err!;

        var request = new CreateCompanyRequest
        {
            Name = name,
            CompanyEmail = companyEmail,
            CompanyWebsite = companyWebsite,
            IndustryUId = indId,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
            AdminEmail = adminEmail
        };

        var response = await commandService.CreateAsync(request, ct);
        var data = response.Data;
        return JsonSerializer.Serialize(new { Id = data?.UId, data?.Name, status = "created" }, Json.Opts);
    }

    [McpServerTool(Name = "update_company"),
     Description(
         "Updates an existing company (full replacement). " +
         "ALWAYS call company_list first to get current values, then include every field. " +
         "Omitted optional fields will be set to null/empty.")]
    public async Task<string> UpdateCompany(
        [Description("The company's GUID from company_list Id field. Never pass a name.")] string companyId,
        [Description("Company name (required)")] string name,
        [Description("Company contact email (required)")] string companyEmail,
        [Description("Industry GUID from industry_list Id field (required)")] string industryUId,
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
        var (ok1, cId, err1) = Json.ParseGuid(companyId, "companyId");
        if (!ok1) return err1!;
        var (ok2, indId, err2) = Json.ParseGuid(industryUId, "industryUId");
        if (!ok2) return err2!;

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
            IndustryUId = indId
        };

        var response = await commandService.UpdateAsync(cId, request, ct);
        var data = response.Data;
        return JsonSerializer.Serialize(new { Id = data?.UId, data?.Name, status = "updated" }, Json.Opts);
    }
}
