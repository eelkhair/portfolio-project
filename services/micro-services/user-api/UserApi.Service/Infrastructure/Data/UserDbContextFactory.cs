using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace UserApi.Infrastructure.Data;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        // Adjust paths if needed (works when running from the Infrastructure project folder)
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("UserDbContext")
                 ?? throw new InvalidOperationException("Connection string 'UserDbContext' not found.");

        var builder = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer(cs, sql =>
            {
                sql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Users");
                sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null);
                // If migrations are stored in Infrastructure, omit MigrationsAssembly.
                // If you store migrations elsewhere, specify: sql.MigrationsAssembly("Your.Migrations.Project");
            });

        return new UserDbContext(builder.Options);
    }
}