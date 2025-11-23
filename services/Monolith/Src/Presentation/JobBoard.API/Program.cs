using JobBoard.API.Infrastructure;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.API.Infrastructure.OpenApi;
using JobBoard.Application;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Infrastructure.Configuration;
using JobBoard.Infrastructure.Diagnostics;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.Smtp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
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
    .AddOpenTelemetryServices(builder.Configuration, "JobBoard.API")
    .AddHealthCheckServices(builder.Configuration)
    .AddHealthChecksUI(c=> c.SetHeaderText("JobBoard - Health Checks"))
    .AddInMemoryStorage();

builder.Build()
    .UseConfiguredSwagger(builder.Configuration)
    .UseHealthCheckServices()
    .UseApplicationServices()
    .Start();