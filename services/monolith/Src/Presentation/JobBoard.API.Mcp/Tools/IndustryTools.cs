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
    [McpServerTool(Name = "industry_list"), Description("Returns a list of all industries in the system.")]
    public async Task<string> ListIndustries(CancellationToken ct)
    {
        var query = new GetIndustriesQuery();
        var result = await dispatcher.DispatchAsync<GetIndustriesQuery, IQueryable<IndustryDto>>(query, ct);
        var industries = await result.ToListAsync(ct);
        return JsonSerializer.Serialize(industries);
    }
}
