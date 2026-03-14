using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.ListByCompany;

public class DraftsByCompanyItemResponse
{
    public List<DraftResponse> Drafts { get; set; } = [];
    public int Count { get; set; }
}

public class ListAllDraftsByCompanyQuery : BaseQuery<Dictionary<Guid, DraftsByCompanyItemResponse>>;

public class ListAllDraftsByCompanyQueryHandler(IJobBoardQueryDbContext context, ILogger<ListAllDraftsByCompanyQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListAllDraftsByCompanyQuery, Dictionary<Guid, DraftsByCompanyItemResponse>>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<Dictionary<Guid, DraftsByCompanyItemResponse>> HandleAsync(
        ListAllDraftsByCompanyQuery request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Fetching all drafts grouped by company...");

        var drafts = await Context.Drafts
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(cancellationToken);

        var grouped = drafts
            .GroupBy(d => d.CompanyId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var items = g.Select(d =>
                    {
                        var response = JsonSerializer.Deserialize<DraftResponse>(d.ContentJson, JsonOpts) ?? new DraftResponse();
                        response.Id = d.Id.ToString();
                        return response;
                    }).ToList();

                    return new DraftsByCompanyItemResponse
                    {
                        Drafts = items,
                        Count = items.Count
                    };
                });

        Activity.Current?.SetTag("companies.count", grouped.Count);
        Activity.Current?.SetTag("drafts.total", drafts.Count);
        Logger.LogInformation("Fetched {DraftCount} drafts across {CompanyCount} companies", drafts.Count, grouped.Count);

        return grouped;
    }
}
