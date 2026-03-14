using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Entities;

public class Draft : BaseAuditableEntity
{
    protected Draft()
    {
        ContentJson = "{}";
        DraftType = "job";
        DraftStatus = "draft";
    }

    private Draft(Guid companyId, string contentJson, string draftType) : this()
    {
        CompanyId = companyId;
        ContentJson = contentJson;
        DraftType = draftType;
        DraftStatus = "generated";
    }

    public Guid CompanyId { get; private set; }
    public string DraftType { get; private set; }
    public string DraftStatus { get; private set; }
    public string ContentJson { get; private set; }

    public void SetContent(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DomainException("Draft.InvalidContent",
                [new Error("Draft.InvalidContent", "Draft content cannot be empty.")]);

        ContentJson = json;
        DraftStatus = "generated";
    }

    public void MarkFinalized()
    {
        DraftStatus = "finalized";
    }

    public static Draft Create(Guid companyId, string contentJson, int internalId, Guid id, string draftType = "job")
    {
        var errors = new List<Error>();

        if (companyId == Guid.Empty)
            errors.Add(new Error("Draft.InvalidCompanyId", "Company ID is required."));

        if (string.IsNullOrWhiteSpace(contentJson))
            errors.Add(new Error("Draft.InvalidContent", "Draft content cannot be empty."));

        if (errors.Count > 0)
            throw new DomainException("Draft.InvalidEntity", errors);

        return new Draft(companyId, contentJson, draftType)
        {
            InternalId = internalId,
            Id = id
        };
    }
}
