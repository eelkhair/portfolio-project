using JobBoard.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.API.Infrastructure.Authorization;
/// <summary>
/// Extension methods for setting up authorization services in an IServiceCollection.
/// </summary>
public static class DependencyInjection
{

    public static IServiceCollection AddAuthorizationService(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
                {
                    configuration.Bind("AzureAd", options);
                    options.TokenValidationParameters.ValidAudiences =
                    [
                        configuration["AzureAd:ClientId"],
                        $"api://{configuration["AzureAd:ClientId"]}"
                    ];
                },
                options => { configuration.Bind("AzureAd", options); });
        services.AddCors(options =>
        {
            options.AddPolicy("AllowMyFrontendApp",
                policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Admin, policy => 
                policy.RequireRole(UserRoles.LabAdmin))
            .AddPolicy(AuthorizationPolicies.Member, policy => 
                policy.RequireRole(UserRoles.LabMember))
            .AddPolicy(AuthorizationPolicies.AllUsers, policy => 
                policy.RequireRole(UserRoles.LabAdmin,UserRoles.LabMember));
        return services;
    }

    public static WebApplication UseApplicationServices(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowMyFrontendApp"); 
        app.UseAuthorization();
        app.MapControllers();
    
       return app;
    }

    public static void Start(this WebApplication app)
    {
        var tracerProvider = app.Services.GetService<TracerProvider>();
        var loggerProvider = app.Services.GetService<LoggerProvider>();

        try
        {
            app.Run();
        }
        finally
        {
            if (tracerProvider != null)
            {
                Console.WriteLine("Flushing and shutting down telemetry traces...");
                tracerProvider.Shutdown();
            }

            if (loggerProvider != null)
            {
                Console.WriteLine("Flushing and shutting down telemetry logs...");
                loggerProvider.Shutdown();
            }
        }

    }

}