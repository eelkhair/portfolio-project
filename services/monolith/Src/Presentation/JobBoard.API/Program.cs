using System.Diagnostics;
using JobBoard.API.Infrastructure;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.Mcp.Common;
using JobBoard.API.Infrastructure.OpenApi;
using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.API.Infrastructure.SignalR.FeatureFlags;
using JobBoard.API.Infrastructure.SignalR.ResumeParse;
using JobBoard.Application;
using JobBoard.Infrastructure.RedisConfig;
using JobBoard.Infrastructure.Diagnostics;
using JobBoard.Infrastructure.HttpClients;
using JobBoard.Infrastructure.Messaging;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.BlobStorage;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Infrastructure.RedisConfig;
using JobBoard.Infrastructure.Smtp;
using JobBoard.Infrastructure.Vault;
using Elkhair.Common.Persistence;
using JobBoard.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// #if DEBUG
//  Debugger.Launch();
// #endif

var isTesting = builder.Environment.IsEnvironment("Testing");
var isAspire = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_MODE"));

builder.Services.AddSingleton<IFeatureFlagNotifier, SignalRFeatureFlagNotifier>();
builder.Services.AddSingleton<ICompanyActivationNotifier, CompanyActivationNotifier>();
builder.Services.AddSingleton<IResumeParseNotifier, ResumeParseNotifier>();

if (isTesting)
{
    builder.Services.AddHealthChecks();
}
else if (isAspire)
{
    (await builder.AddRedisConfiguration("monolith-api", TimeSpan.FromSeconds(5)))
        .ConfigureLogging("monolith-api")
        .AddCustomHealthChecks();
    builder.Services.AddAppConfigurationServices();
    builder.Services.AddMassTransitMessaging(builder.Configuration);
    builder.Services.AddAiServiceHttpClient(builder.Configuration);
}
else
{
    builder.AddVaultSecrets("monolith");
    (await builder.AddRedisConfiguration("monolith-api", TimeSpan.FromSeconds(5)))
        .ConfigureLogging("monolith-api")
        .AddCustomHealthChecks();
    builder.Services.AddMassTransitMessaging(builder.Configuration);
    builder.Services.AddAiServiceHttpClient(builder.Configuration);
}

builder.Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<IUserAccessor, HttpUserAccessor>()
    .AddOutboxServices()
    .AddODataServices()
    .AddAuthorizationService(builder.Configuration)
    .AddSmtpServices(builder.Configuration)
    .AddBlobStorageServices(builder.Configuration)
    .AddConfiguredSwagger(builder.Configuration)
    .AddOpenTelemetryServices(builder.Configuration, "monolith-api")
    .AddSignalR();

var app = builder.Build();

if (!isTesting)
{
    await app.MigrateDatabase<JobBoardDbContext>();

    using var seedScope = app.Services.CreateScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<JobBoardDbContext>();
    var hasIndustries = await seedDb.Database.SqlQueryRaw<int>("SELECT 1 AS Value FROM [Company].[Industries]").AnyAsync();
    if (!hasIndustries)
    {
        await seedDb.Database.ExecuteSqlRawAsync(@"
            INSERT INTO [Company].[Industries] ([Id], [Name], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
            SELECT v.[Id], v.[Name], SYSUTCDATETIME(), 'seed', SYSUTCDATETIME(), 'seed'
            FROM (VALUES
             ('A1B2C3D4-0001-4000-8000-000000000001', 'Technology'),
             ('A1B2C3D4-0002-4000-8000-000000000002', 'Healthcare'),
             ('A1B2C3D4-0003-4000-8000-000000000003', 'Finance'),
             ('A1B2C3D4-0004-4000-8000-000000000004', 'Education'),
             ('A1B2C3D4-0005-4000-8000-000000000005', 'Manufacturing'),
             ('A1B2C3D4-0006-4000-8000-000000000006', 'Retail'),
             ('A1B2C3D4-0007-4000-8000-000000000007', 'Construction'),
             ('A1B2C3D4-0008-4000-8000-000000000008', 'Transportation & Logistics'),
             ('A1B2C3D4-0009-4000-8000-000000000009', 'Energy'),
             ('A1B2C3D4-000A-4000-8000-00000000000A', 'Hospitality & Tourism'),
             ('A1B2C3D4-000B-4000-8000-00000000000B', 'Real Estate'),
             ('A1B2C3D4-000C-4000-8000-00000000000C', 'Media & Entertainment'),
             ('A1B2C3D4-000D-4000-8000-00000000000D', 'Agriculture'),
             ('A1B2C3D4-000E-4000-8000-00000000000E', 'Nonprofit & Government'),
             ('A1B2C3D4-000F-4000-8000-00000000000F', 'Insurance'),
             ('A1B2C3D4-0010-4000-8000-000000000010', 'Other')
            ) AS v([Id], [Name])");
    }
}

app.UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program;