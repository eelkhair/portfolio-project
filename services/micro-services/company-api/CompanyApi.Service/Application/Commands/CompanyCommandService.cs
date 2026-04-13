using System.Security.Claims;
using CompanyApi.Application.Commands.Interfaces;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Elkhair.Dev.Common.Dapr;
using JobBoard.IntegrationEvents.Company;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Commands;

public partial class CompanyCommandService(ICompanyDbContext context, IMessageSender messageSender, ILogger<CompanyCommandService> logger) : ICompanyCommandService
{
    private static readonly TypeAdapterConfig IgnoreIndustryConfig = TypeAdapterConfig.GlobalSettings.Fork(c =>
        c.NewConfig<Company, CompanyResponse>().Ignore(dest => dest.IndustryUId));

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        LogCreatingCompany(logger, request.Name);

        var company = request.Adapt<Company>();
        company.Status = "Provisioning";
        company.IndustryId = await context.Industries.Where(c => c.UId == request.IndustryUId).Select(c => c.Id)
            .FirstAsync(ct);
        if (request.CompanyId.HasValue)
        {
            company.UId = request.CompanyId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            company.CreatedBy = request.UserId;
            company.UpdatedBy = request.UserId;
        }
        context.Companies.Add(company);

        await context.SaveChangesAsync(user, ct);

        var response = company.Adapt<CompanyResponse>(IgnoreIndustryConfig);
        response.IndustryUId = request.IndustryUId;

        LogCompanyCreated(logger, company.UId);
        return response;

    }

    public async Task<CompanyResponse> UpdateAsync(Guid companyUId, UpdateCompanyRequest request, ClaimsPrincipal user, CancellationToken ct, bool publishEvent = true)
    {
        LogUpdatingCompany(logger, companyUId);

        var company = await context.Companies.Include(c => c.Industry).SingleAsync(c => c.UId == companyUId, ct);

        company.Name = request.Name;
        company.Email = request.CompanyEmail;
        company.Website = request.CompanyWebsite;
        company.Phone = request.Phone;
        company.Description = request.Description;
        company.About = request.About;
        company.EEO = request.EEO;
        company.Founded = request.Founded;
        company.Size = request.Size;
        company.Logo = request.Logo;
        company.IndustryId = await context.Industries
            .Where(i => i.UId == request.IndustryUId)
            .Select(i => i.Id)
            .FirstAsync(ct);

        await context.SaveChangesAsync(user, ct);

        if (publishEvent)
        {
            var evt = new MicroCompanyUpdatedV1Event(
                companyUId, company.Name, company.Email, company.Website,
                company.Phone, company.Description, company.About, company.EEO,
                company.Founded, company.Size, company.Logo, request.IndustryUId)
            {
                UserId = user.FindFirst("sub")?.Value ?? "system"
            };
            await messageSender.SendEventAsync("rabbitmq.pubsub", "micro.company-updated.v1",
                evt.UserId, evt, ct);
        }

        var response = company.Adapt<CompanyResponse>();
        response.IndustryUId = request.IndustryUId;

        LogCompanyUpdated(logger, companyUId, publishEvent);
        return response;
    }

    public async Task<bool> ActivateAsync(Guid companyUId, ClaimsPrincipal user, CancellationToken ct)
    {
        LogActivatingCompany(logger, companyUId);

        var company = await context.Companies.Where(c => c.UId == companyUId).SingleOrDefaultAsync(ct);

        if (company == null)
        {
            LogCompanyNotFound(logger, companyUId);
            return false;
        }

        company.Status = "Active";
        context.Companies.Update(company);
        await context.SaveChangesAsync(user, ct);

        LogCompanyActivated(logger, companyUId);
        return true;
    }

    [LoggerMessage(LogLevel.Information, "Creating company '{Name}'")]
    static partial void LogCreatingCompany(ILogger logger, string name);

    [LoggerMessage(LogLevel.Information, "Company created with UId {CompanyUId}")]
    static partial void LogCompanyCreated(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Updating company {CompanyUId}")]
    static partial void LogUpdatingCompany(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Company {CompanyUId} updated, event published: {EventPublished}")]
    static partial void LogCompanyUpdated(ILogger logger, Guid companyUId, bool eventPublished);

    [LoggerMessage(LogLevel.Information, "Activating company {CompanyUId}")]
    static partial void LogActivatingCompany(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Warning, "Company {CompanyUId} not found for activation")]
    static partial void LogCompanyNotFound(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Company {CompanyUId} activated")]
    static partial void LogCompanyActivated(ILogger logger, Guid companyUId);
}
