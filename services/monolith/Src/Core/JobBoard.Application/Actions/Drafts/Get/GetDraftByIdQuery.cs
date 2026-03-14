using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Exceptions;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.Get;

public class GetDraftByIdQuery : BaseQuery<DraftResponse>
{
    public Guid DraftId { get; set; }
}

public class GetDraftByIdQueryHandler(IJobBoardQueryDbContext context, ILogger<GetDraftByIdQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetDraftByIdQuery, DraftResponse>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DraftResponse> HandleAsync(GetDraftByIdQuery request, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("draft.id", request.DraftId);
        Logger.LogInformation("Fetching draft {DraftId}...", request.DraftId);

        var draft = await Context.Drafts
            .FirstOrDefaultAsync(d => d.Id == request.DraftId, cancellationToken);

        if (draft is null)
            throw new DomainException("Draft.NotFound",
                [new Error("Draft.NotFound", $"Draft '{request.DraftId}' not found.")]);

        var response = JsonSerializer.Deserialize<DraftResponse>(draft.ContentJson, JsonOpts) ?? new DraftResponse();
        response.Id = draft.Id.ToString();

        Logger.LogInformation("Fetched draft {DraftId}", request.DraftId);
        return response;
    }
}
