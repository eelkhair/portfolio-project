using JobBoard.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace JobBoard.Monolith.Tests.Integration.Fixtures;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Run EF migrations to create the schema
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public JobBoardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobBoardDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new JobBoardDbContext(options);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>;
