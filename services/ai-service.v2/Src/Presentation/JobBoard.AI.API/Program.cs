using System.Diagnostics;
using JobBoard.AI.API.Infrastructure;
using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.API.Infrastructure.OpenApi;
using JobBoard.AI.API.Infrastructure.SignalR;
using JobBoard.AI.Application;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Infrastructure.AI;
using JobBoard.AI.Infrastructure.Configuration;
using JobBoard.AI.Infrastructure.Dapr;
using JobBoard.AI.Infrastructure.HttpClients;
using Azure.Storage.Blobs;
using JobBoard.AI.Infrastructure.Diagnostics;
using JobBoard.AI.Infrastructure.Persistence;
using Elkhair.Common.Persistence;

var builder = WebApplication.CreateBuilder(args);

// #if DEBUG
// Debugger.Launch();
// #endif
(await builder.AddDaprServices("ai-service-v2")).ConfigureLogging("ai-service-v2").AddCustomHealthChecks().Services
    .AddApplicationServices()
    .AddConfigurationServices(builder.Configuration)
    .AddAiServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration)
    .AddHttpContextAccessor()
    .AddAuthorizationService(builder.Configuration)
    .AddConfiguredSwagger(builder.Configuration)
    .AddOpenTelemetryServices(builder.Configuration, "ai-service-v2")
    .AddSignalR();

builder.Services.AddMonolithHttpClient(builder.Configuration);

builder.Services.AddScoped<IAiNotificationHub, AiNotificationHubNotifier>();

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

builder.Services.AddMcpToolProviders(builder.Configuration);

var app = builder.Build();
await app.MigrateDatabase<AiDbContext>();
app.UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();