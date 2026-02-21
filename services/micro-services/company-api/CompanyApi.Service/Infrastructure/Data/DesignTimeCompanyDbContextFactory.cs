using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CompanyApi.Infrastructure.Data;

public class DesignTimeCompanyDbContextFactory :  IDesignTimeDbContextFactory<CompanyDbContext>
{
    public CompanyDbContext CreateDbContext(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CompanyDbContext>();
        optionsBuilder.UseSqlServer(config.GetConnectionString("CompanyDbContext"), sql =>
        {
            sql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Companies");
        });

        return new CompanyDbContext(optionsBuilder.Options);
    }
}
