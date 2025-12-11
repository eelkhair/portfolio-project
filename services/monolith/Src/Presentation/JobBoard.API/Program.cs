using System.Diagnostics;
using JobBoard.API.Infrastructure;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.API.Infrastructure.OpenApi;
using JobBoard.Application;
using JobBoard.Application.Interfaces.Users;
using JobBoard.infrastructure.Dapr;
using JobBoard.Infrastructure.Diagnostics;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.Smtp;

var builder = WebApplication.CreateBuilder(args);
#if DEBUG
Debugger.Launch();
#endif

(await builder.AddDaprServices("monolith-api")).ConfigureLogging("monolith-api").AddCustomHealthChecks().Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddKafkaServices(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<IUserAccessor, HttpUserAccessor>()
    .AddOutboxServices()
    .AddODataServices()
    .AddAuthorizationService(builder.Configuration)
    .AddSmtpServices(builder.Configuration)
    .AddConfiguredSwagger(builder.Configuration)
    .AddOpenTelemetryServices(builder.Configuration, "monolith-api");

builder.Build().UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();