using AdminApi.Core;
using AdminApi.Mcp.Infrastructure;
using Elkhair.Common.Observability;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprCoreServices("admin-api")).ConfigureLogging("admin-api-mcp");

builder.Services.AddAdminMcpServices(builder.Configuration);

var app = builder.Build();
app.UseAdminMcpPipeline();

await app.RunAsync();
