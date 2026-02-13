using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.List;

public class ListDraftsByLocationQuery(Guid companyId, string location) : BaseQuery<List<DraftResponse>>
{
    public Guid CompanyId { get; } = companyId;
    public string Location { get; } = location;
}

public class ListDraftsByLocationQueryHandler(ILogger<ListDraftsByLocationQuery> logger, IAiDbContext context) : BaseQueryHandler(logger), IHandler<ListDraftsByLocationQuery, List<DraftResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<List<DraftResponse>> HandleAsync(ListDraftsByLocationQuery request, CancellationToken cancellationToken)
    {
        var drafts =  await context.Drafts.Where(d => d.CompanyId == request.CompanyId).ToListAsync(cancellationToken);
        
        var responses = new List<DraftResponse>();
        foreach (var draft in drafts)
        {
            if (!draft.ContentJson.Contains(request.Location, StringComparison.OrdinalIgnoreCase))
                continue;
            
            var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOptions);
            
            if (response == null) continue;
            
            responses.Add(response);
            response.Id = draft.Id.ToString();

        }
        return responses.Where(c=> c.Location.Contains(request.Location)).ToList();
    }
}