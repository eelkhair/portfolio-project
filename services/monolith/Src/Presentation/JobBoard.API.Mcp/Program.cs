using Elkhair.Common.Observability;
using JobBoard.API.Mcp.Infrastructure;
using JobBoard.Infrastructure.RedisConfig;
using JobBoard.Infrastructure.Vault;

var builder = WebApplication.CreateBuilder(args);

var isAspire = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_MODE"));

if (isAspire)
{
    (await builder.AddRedisConfiguration("monolith-mcp", TimeSpan.FromSeconds(5)))
        .ConfigureLogging("monolith-mcp");
}
else
{
    builder.AddVaultSecrets("monolith");
    (await builder.AddRedisConfiguration("monolith-mcp", TimeSpan.FromSeconds(5)))
        .ConfigureLogging("monolith-mcp");
}

builder.Services.AddMonolithMcpServices(builder.Configuration);

var app = builder.Build();
app.UseMonolithMcpPipeline();

await app.RunAsync();
