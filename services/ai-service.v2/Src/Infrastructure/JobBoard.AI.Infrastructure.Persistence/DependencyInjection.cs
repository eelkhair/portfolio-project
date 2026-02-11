using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AiDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("ai-db"), o => o.UseVector()    );
        });
        
        services.AddScoped<IAiDbContext, AiDbContext>();
        
        return services;
    }
}
