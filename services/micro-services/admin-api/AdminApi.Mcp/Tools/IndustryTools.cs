using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Services;
using ModelContextProtocol.Server;

namespace AdminApi.Mcp.Tools;

[McpServerToolType]
public class IndustryTools(IIndustryQueryService queryService)
{
    [McpServerTool(Name = "industry_list"), Description("Returns a list of all industries in the system.")]
    public async Task<string> ListIndustries(CancellationToken ct)
    {
        var response = await queryService.ListAsync(ct);
        return JsonSerializer.Serialize(response.Data);
    }
}
