using Elkhair.Common.Observability;
using Elkhair.Common.Persistence;
using UserApi.Infrastructure;
using UserApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("user-api")).ConfigureLogging("user-api");

builder.Services.AddUserApiServices(builder.Configuration);
builder.AddCustomHealthChecks();

var app = builder.Build();
await app.MigrateDatabase<UserDbContext>();
app.UseUserApiPipeline(builder.Configuration);

await app.RunAsync();
