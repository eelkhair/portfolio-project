using AdminAPI.Contracts.Models.Jobs.Requests;
using FastEndpoints;
using FluentValidation;

namespace AdminApi.Features.Jobs.DraftUpsert;

public sealed class UpsertDraftValidator: Validator<JobDraftRequest>
{
    public UpsertDraftValidator()
    {
        When(c => c.Metadata != null, () =>
        {
            RuleFor(x => x.Metadata.RoleLevel)
                .IsInEnum();
       
             RuleFor(x => x.Metadata.Tone)
                        .IsInEnum();
        });
       
    }
}