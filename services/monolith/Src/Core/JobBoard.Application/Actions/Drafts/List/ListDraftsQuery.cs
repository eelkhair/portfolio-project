using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.List;

public class ListDraftsQuery : BaseQuery<List<DraftResponse>>
{
    public Guid CompanyId { get; set; }
}

public class ListDraftsQueryHandler(IJobBoardQueryDbContext context, ILogger<ListDraftsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListDraftsQuery, List<DraftResponse>>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<List<DraftResponse>> HandleAsync(ListDraftsQuery request, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("company.id", request.CompanyId);
        Logger.LogInformation("Fetching drafts for company {CompanyId}...", request.CompanyId);

        var drafts = await Context.Drafts
            .Where(d => d.CompanyId == request.CompanyId)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(cancellationToken);

        var result = drafts.Select(d =>
        {
            var response = JsonSerializer.Deserialize<DraftResponse>(d.ContentJson, JsonOpts) ?? new DraftResponse();
            response.Id = d.Id.ToString();
            return response;
        }).ToList();

        Logger.LogInformation("Fetched {DraftCount} drafts for company {CompanyId}", result.Count, request.CompanyId);
        Activity.Current?.SetTag("drafts.count", result.Count);
        return result;
    }
}
