using System.ComponentModel;
using System.Text.Json;
using JobBoard.API.Mcp.Infrastructure;
using JobBoard.Application.Actions.Companies.Industries;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace JobBoard.API.Mcp.Tools;

[McpServerToolType]
public class IndustryTools(HandlerDispatcher dispatcher)
{
    [McpServerTool(Name = "industry_list"), Description("Returns all industries (id + name).")]
    public async Task<string> ListIndustries(CancellationToken ct)
    {
        var query = new GetIndustriesQuery();
        var result = await dispatcher.DispatchAsync<GetIndustriesQuery, IQueryable<IndustryDto>>(query, ct);
        var industries = await result.Select(i => new { i.Id, i.Name }).ToListAsync(ct);
        return JsonSerializer.Serialize(industries, Json.Opts);
    }
}
