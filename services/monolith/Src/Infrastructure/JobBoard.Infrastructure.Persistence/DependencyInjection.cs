using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Monolith") ?? throw new InvalidOperationException("Connection string 'Monolith' not found.");
   
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Monolith' not found.");
        }
        services.AddDbContext<JobBoardDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        services.AddScoped<IJobBoardDbContext>(p =>
            p.GetRequiredService<JobBoardDbContext>());
        services.AddScoped<IUnitOfWork>(p => 
            p.GetRequiredService<JobBoardDbContext>());
        services.AddScoped<IJobBoardQueryDbContext>(provider =>
            provider.GetRequiredService<JobBoardDbContext>()); 
        services.AddScoped<ITransactionDbContext>(provider =>
            provider.GetRequiredService<JobBoardDbContext>()); 
        services.AddScoped<IOutboxDbContext>(provider =>
            provider.GetRequiredService<JobBoardDbContext>());

        services.Scan(scan => scan
            .FromAssemblyOf<UserRepository>()
            .AddClasses(classes => classes.AssignableTo<BaseRepository>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        
        return services;
    }
}