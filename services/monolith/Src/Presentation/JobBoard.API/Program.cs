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
using Elkhair.Common.Observability;
using JobBoard.Infrastructure.HttpClients;
using JobBoard.Infrastructure.Messaging;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.BlobStorage;
using JobBoard.Infrastructure.Persistence.Context;
using JobBoard.Infrastructure.Smtp;
using JobBoard.Infrastructure.Vault;
using Elkhair.Common.Persistence;
using JobBoard.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

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
    .AddDiagnosticsServices(builder.Configuration, "monolith-api")
    .AddSignalR();

var app = builder.Build();

if (!isTesting)
{
    if (isAspire) await Task.Delay(TimeSpan.FromSeconds(10));
    await app.MigrateDatabase<JobBoardDbContext>();
    await app.SeedIndustriesAsync();
}

app.UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program;
