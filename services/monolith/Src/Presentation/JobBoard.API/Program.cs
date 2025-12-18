using System.Diagnostics;
using JobBoard.API.Infrastructure;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.API.Infrastructure.OpenApi;
using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.API.Infrastructure.SignalR.FeatureFlags;
using JobBoard.Application;
using JobBoard.Application.Interfaces.Users;
using JobBoard.infrastructure.Dapr;
using JobBoard.Infrastructure.Dapr;
using JobBoard.Infrastructure.Diagnostics;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.Smtp;

var builder = WebApplication.CreateBuilder(args);
#if DEBUG
Debugger.Launch();
#endif
builder.Services.AddSingleton<IFeatureFlagNotifier, SignalRFeatureFlagNotifier>();
builder.Services.AddSingleton<ICompanyActivationNotifier, CompanyActivationNotifier>();
(await builder.AddDaprServices("monolith-api")).ConfigureLogging("monolith-api").AddCustomHealthChecks().Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<IUserAccessor, HttpUserAccessor>()
    .AddOutboxServices()
    .AddODataServices()
    .AddAuthorizationService(builder.Configuration)
    .AddSmtpServices(builder.Configuration)
    .AddConfiguredSwagger(builder.Configuration)
    .AddOpenTelemetryServices(builder.Configuration, "monolith-api")
    .AddSignalR();

builder.Build().UseConfiguredSwagger(builder.Configuration)
    .UseApplicationServices()
    .Start();