using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace JobApi.Infrastructure.Data;

public class DesignTimeJobDbContextFactory : IDesignTimeDbContextFactory<JobDbContext>
{
    public JobDbContext CreateDbContext(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<JobDbContext>();
        optionsBuilder.UseSqlServer(config.GetConnectionString("JobDbContext"), sql =>
        {
            sql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Jobs");
        });

        return new JobDbContext(optionsBuilder.Options);
    }
}