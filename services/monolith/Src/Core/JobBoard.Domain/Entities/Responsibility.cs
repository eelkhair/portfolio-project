using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Responsibility;

namespace JobBoard.Domain.Entities;

public class Responsibility : BaseAuditableEntity
{
    protected Responsibility()
    {
        Value = string.Empty;
    }

    private Responsibility(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }
    public int JobId { get; private set; }
    public Job Job { get; private set; } = null!;

    public void SetValue(string value)
    {
        Value = ResponsibilityValue.Create(value)
            .Ensure<ResponsibilityValue, string>("Responsibility.InvalidValue")!;
    }

    internal void SetJob(int jobId)
    {
        JobId = jobId;
    }

    public static Responsibility Create(
        string valueInput,
        DateTime? createdAt = null,
        string? createdBy = null)
    {
        var responsibility = ValidateAndCreate(valueInput);
        EntityFactory.ApplyAudit(responsibility, createdAt, createdBy);
        return responsibility;
    }

    private static Responsibility ValidateAndCreate(string valueInput)
    {
        var errors = new List<Error>();

        var value = ResponsibilityValue.Create(valueInput)
            .Collect<ResponsibilityValue, string>(errors)!;

        if (errors.Count != 0)
        {
            throw new DomainException("Responsibility.InvalidEntity", errors);
        }

        return new Responsibility(value);
    }
}