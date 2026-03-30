using Elkhair.Common.Observability;
using Elkhair.Common.Persistence;
using JobApi.Infrastructure;
using JobApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("job-api")).ConfigureLogging("job-api");

builder.Services.AddJobApiServices(builder.Configuration);
builder.AddCustomHealthChecks();

var app = builder.Build();
await app.MigrateDatabase<JobDbContext>();
app.UseJobApiPipeline(builder.Configuration);

await app.RunAsync();
