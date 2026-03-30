using CompanyApi.Infrastructure;
using CompanyApi.Infrastructure.Data;
using Elkhair.Common.Observability;
using Elkhair.Common.Persistence;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("company-api")).ConfigureLogging("company-api");

builder.Services.AddCompanyApiServices(builder.Configuration);
builder.AddCustomHealthChecks();

var app = builder.Build();
await app.MigrateDatabase<CompanyDbContext>();
app.UseCompanyApiPipeline(builder.Configuration);

await app.RunAsync();
