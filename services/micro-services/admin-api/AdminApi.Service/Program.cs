using AdminApi.Infrastructure;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("admin-api")).ConfigureLogging("admin-api");

builder.Services.AddAdminApiServices(builder.Configuration);
builder.AddCustomHealthChecks();

var app = builder.Build();
app.UseAdminApiPipeline(builder.Configuration);

await app.RunAsync();
