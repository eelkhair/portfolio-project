using AdminApi.Application.Commands;
using AdminApi.Application.Queries;
using AdminAPI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AdminApi.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminApiCoreServices(this IServiceCollection services)
    {
        services.AddScoped<ICompanyQueryService, CompanyQueryService>();
        services.AddScoped<ICompanyCommandService, CompanyCommandService>();
        services.AddScoped<IIndustryQueryService, IndustryQueryService>();
        services.AddScoped<IJobQueryService, JobQueryService>();
        services.AddScoped<IOpenAICommandService, OpenAICommandService>();
        services.AddScoped<IJobCommandService, JobCommandService>();
        services.AddScoped<ISettingsCommandService, SettingsCommandService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        return services;
    }
}
