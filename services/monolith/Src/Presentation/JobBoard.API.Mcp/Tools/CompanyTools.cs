using System.ComponentModel;
using System.Text.Json;
using JobBoard.API.Mcp.Infrastructure;
using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Application.Actions.Companies.Get;
using JobBoard.Application.Actions.Companies.Update;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace JobBoard.API.Mcp.Tools;

[McpServerToolType]
public class CompanyTools(HandlerDispatcher dispatcher)
{
    [McpServerTool(Name = "company_list"), Description("Returns a list of all companies (id, name, email, industry).")]
    public async Task<string> ListCompanies(CancellationToken ct)
    {
        var query = new GetCompaniesQuery();
        var result = await dispatcher.DispatchAsync<GetCompaniesQuery, IQueryable<CompanyDto>>(query, ct);
        var companies = await result.Select(c => new
        {
            c.Id,
            c.Name,
            c.Email,
            Industry = c.Industry != null ? c.Industry.Name : null
        }).ToListAsync(ct);
        return JsonSerializer.Serialize(companies, Json.Opts);
    }

    [McpServerTool(Name = "company_detail"),
     Description("Returns full details for a single company by ID. Use after company_list to get description, about, website, phone, size, etc.")]
    public async Task<string> GetCompany(
        [Description("The company's unique identifier")] Guid companyId,
        CancellationToken ct)
    {
        var query = new GetCompaniesQuery();
        var result = await dispatcher.DispatchAsync<GetCompaniesQuery, IQueryable<CompanyDto>>(query, ct);
        var company = await result.Where(c => c.Id == companyId).Select(c => new
        {
            c.Id,
            c.Name,
            c.Email,
            c.Description,
            c.About,
            c.Website,
            c.Phone,
            c.Founded,
            c.Size,
            c.EEO,
            Industry = c.Industry != null ? c.Industry.Name : null
        }).FirstOrDefaultAsync(ct);

        return company is not null
            ? JsonSerializer.Serialize(company, Json.Opts)
            : JsonSerializer.Serialize(new { error = "Company not found." }, Json.Opts);
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
        var command = new CreateCompanyCommand
        {
            Name = name,
            CompanyEmail = companyEmail,
            CompanyWebsite = companyWebsite,
            IndustryUId = industryUId,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
            AdminEmail = adminEmail
        };

        var result = await dispatcher.DispatchAsync<CreateCompanyCommand, CompanyDto>(command, ct);
        return JsonSerializer.Serialize(new { result.Id, result.Name, status = "created" }, Json.Opts);
    }

    [McpServerTool(Name = "update_company"),
     Description(
         "Updates an existing company (full replacement). " +
         "ALWAYS call company_list first to get current values, then include every field. " +
         "Omitted optional fields will be set to null/empty.")]
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
        var command = new UpdateCompanyCommand
        {
            Id = companyId,
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

        var result = await dispatcher.DispatchAsync<UpdateCompanyCommand, CompanyDto>(command, ct);
        return JsonSerializer.Serialize(new { result.Id, result.Name, status = "updated" }, Json.Opts);
    }
}
