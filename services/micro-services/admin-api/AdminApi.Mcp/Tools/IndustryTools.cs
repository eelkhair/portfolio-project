using System.ComponentModel;
using System.Text.Json;
using AdminAPI.Contracts.Services;
using ModelContextProtocol.Server;

namespace AdminApi.Mcp.Tools;

[McpServerToolType]
public class IndustryTools(IIndustryQueryService queryService)
{
    [McpServerTool(Name = "industry_list"), Description("Returns all industries (id + name).")]
    public async Task<string> ListIndustries(CancellationToken ct)
    {
        var response = await queryService.ListAsync(ct);
        var slim = response.Data?.Select(i => new { i.UId, i.Name }) ?? [];
        return JsonSerializer.Serialize(slim);
    }
}
