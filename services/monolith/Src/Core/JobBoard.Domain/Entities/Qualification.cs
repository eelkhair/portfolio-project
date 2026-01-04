using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Qualification;

namespace JobBoard.Domain.Entities;

public class Qualification : BaseAuditableEntity
{
    protected Qualification()
    {
        Value = string.Empty;
    }

    private Qualification(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }
    public int JobId { get; private set; }
    public Job Job { get; private set; } = null!;

    public void SetValue(string value)
    {
        Value = QualificationValue.Create(value)
            .Ensure<QualificationValue, string>("Qualification.InvalidValue")!;
    }

    internal void SetJob(int jobId)
    {
        JobId = jobId;
    }

    public static Qualification Create(
        string valueInput,
        DateTime? createdAt = null,
        string? createdBy = null)
    {
        var qualification = ValidateAndCreate(valueInput);
        EntityFactory.ApplyAudit(qualification, createdAt, createdBy);
        return qualification;
    }

    private static Qualification ValidateAndCreate(string valueInput)
    {
        var errors = new List<Error>();

        var value = QualificationValue.Create(valueInput)
            .Collect<QualificationValue, string>(errors)!;

        if (errors.Count != 0)
        {
            throw new DomainException("Qualification.InvalidEntity", errors);
        }

        return new Qualification(value);
    }
}