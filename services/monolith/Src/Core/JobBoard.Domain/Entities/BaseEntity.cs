// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace JobBoard.Domain.Entities;

public abstract class BaseEntity
{
    public int InternalId { get; set; }
    public Guid Id { get; set; }
}