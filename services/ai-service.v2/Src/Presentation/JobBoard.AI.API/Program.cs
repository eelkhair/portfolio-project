using System.Diagnostics;
using JobBoard.AI.API.Infrastructure;
using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.API.Infrastructure.OpenApi;
using JobBoard.AI.Application;
using JobBoard.AI.Infrastructure.AI;
using JobBoard.AI.Infrastructure.Configuration;
using JobBoard.AI.Infrastructure.Dapr;
using JobBoard.AI.Infrastructure.Diagnostics;
using JobBoard.AI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
Debugger.Launch();
#endif
(await builder.AddDaprServices("ai-service-v2")).ConfigureLogging("ai-service-v2").AddCustomHealthChecks().Services
    .AddApplicationServices()
    .AddConfigurationServices()
    .AddAiServices(builder.Configuration)
    .AddPersistenceServices()
    .AddHttpContextAccessor()
    .AddAuthorizationService(builder.Configuration)
    .AddConfiguredSwagger(builder.Configuration)
    .AddOpenTelemetryServices(builder.Configuration, "ai-service-v2")
    .AddSignalR();

builder.Build().UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();