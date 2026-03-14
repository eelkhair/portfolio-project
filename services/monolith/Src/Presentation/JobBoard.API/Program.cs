using System.Diagnostics;
using JobBoard.API.Infrastructure;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.API.Infrastructure.OpenApi;
using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.API.Infrastructure.SignalR.FeatureFlags;
using JobBoard.API.Infrastructure.SignalR.ResumeParse;
using JobBoard.Application;
using JobBoard.Application.Interfaces.Users;
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
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// #if DEBUG
//  Debugger.Launch();
// #endif

var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddSingleton<IFeatureFlagNotifier, SignalRFeatureFlagNotifier>();
builder.Services.AddSingleton<ICompanyActivationNotifier, CompanyActivationNotifier>();
builder.Services.AddSingleton<IResumeParseNotifier, ResumeParseNotifier>();

if (isTesting)
{
    builder.Services.AddHealthChecks();
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
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<JobBoardDbContext>();
    await db.Database.MigrateAsync();
}

app.UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program;