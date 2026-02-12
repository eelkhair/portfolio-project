using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.List;

public class ListDraftsQuery(Guid companyId) : BaseQuery<List<DraftResponse>>
{
    public Guid CompanyId { get; } = companyId;
}

public class ListDraftsQueryHandler(ILogger<ListDraftsQuery> logger, IAiDbContext context) : BaseQueryHandler(logger), IHandler<ListDraftsQuery, List< DraftResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<List<DraftResponse>> HandleAsync(ListDraftsQuery request, CancellationToken cancellationToken)
    {
        var drafts =  await context.Drafts.Where(d => d.CompanyId == request.CompanyId).ToListAsync(cancellationToken);
        
        var responses = new List<DraftResponse>();
        foreach (var draft in drafts)
        {
            var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOptions);

            if (response != null)
            {
                responses.Add(response);
                response.Id = draft.Id.ToString();
            }
            
        }
        return responses;
    }
}