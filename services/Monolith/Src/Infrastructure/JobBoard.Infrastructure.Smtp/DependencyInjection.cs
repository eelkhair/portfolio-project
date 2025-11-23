using JobBoard.Application.Interfaces.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorLight;

namespace JobBoard.Infrastructure.Smtp;

public static class DependencyInjection
{
    public static IServiceCollection AddSmtpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.AddSingleton<IRazorLightEngine>(provider =>
        {
     
            var hostEnvironment = provider.GetRequiredService<IHostEnvironment>();
            var templateFolderPath = Path.Combine(hostEnvironment.ContentRootPath, "Templates");

            var engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(templateFolderPath)
                .UseMemoryCachingProvider()
                .Build();

            return engine;
        });

        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

        return services;
    }
}