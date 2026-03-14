using System.ComponentModel;
using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Clients;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Configuration;

namespace JobBoard.AI.MCP.Integration.Tools.Admin;

[McpServerToolType]
public class IndustryTools(
    IAdminApiClient adminClient,
    IMonolithApiClient monolithClient,
    IConfiguration configuration)
{
    private bool IsMonolith => configuration.GetValue<bool>("FeatureFlags:Monolith");

    [McpServerTool(Name = "industry_list"), Description("Returns a list of all industries in the system.")]
    public async Task<string> ListIndustries(CancellationToken ct)
    {
        if (IsMonolith)
        {
            var result = await monolithClient.ListIndustriesAsync(ct);
            return JsonSerializer.Serialize(result.Value);
        }

        var response = await adminClient.ListIndustriesAsync(ct);
        return JsonSerializer.Serialize(response.Data);
    }
}
