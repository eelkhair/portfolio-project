using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.DraftsByCompany;

public class DraftsByCompanyQuery : BaseQuery<DraftsByCompanyResponse>;

public class DraftsByCompanyQueryHandler(ILogger<DraftsByCompanyQuery> logger, IAiDbContext context) : BaseQueryHandler(logger),
    IHandler<DraftsByCompanyQuery, DraftsByCompanyResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<DraftsByCompanyResponse> HandleAsync(DraftsByCompanyQuery request, CancellationToken cancellationToken)
    {
        var drafts = await context.Drafts.ToListAsync(cancellationToken);
        
        var result = new DraftsByCompanyResponse();
        
        var groupedDrafts = drafts.GroupBy(d => d.CompanyId);

        foreach (var companyGroup in groupedDrafts)
        {
            var responses = new List<DraftResponse>();

            foreach (var draft in companyGroup)
            {
                var response = JsonSerializer.Deserialize<DraftResponse>(
                    draft.ContentJson,
                    JsonOptions);

                if (response is null)
                    continue;

                response.Id = draft.Id.ToString();
                responses.Add(response);
            }

            result.DraftsByCompany[companyGroup.Key] =
                new DraftsByCompanyItemResponse
                {
                    Drafts = responses
                };
        }
        return result;
    }
}