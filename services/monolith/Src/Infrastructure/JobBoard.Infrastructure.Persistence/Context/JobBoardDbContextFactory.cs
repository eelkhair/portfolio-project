using JobBoard.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
// ReSharper disable UnusedType.Global
namespace JobBoard.Infrastructure.Persistence.Context;

public class JobBoardDbContextFactory : IDesignTimeDbContextFactory<JobBoardDbContext>
{
    public JobBoardDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var solutionRoot = GetSolutionRoot(currentDirectory) ?? throw new DirectoryNotFoundException("Could not find the solution root directory.");
        var apiProjectPath = Path.Combine(solutionRoot, "Src", "Presentation", "JobBoard.API");

        if (!Directory.Exists(apiProjectPath))
        {
            throw new DirectoryNotFoundException($"The API project path was not found at the expected location: {apiProjectPath}");
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Monolith");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("The 'Monolith' was not found in the API project's appsettings for design-time use.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<JobBoardDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        return new JobBoardDbContext(optionsBuilder.Options);
    }

    private static string? GetSolutionRoot(string currentDir)
    {
        var dir = new DirectoryInfo(currentDir);
        while (dir != null && dir.GetFiles("*.sln").Length == 0)
        {
            dir = dir.Parent;
        }
        return dir?.FullName;
    }
}