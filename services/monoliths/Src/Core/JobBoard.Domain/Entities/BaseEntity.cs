// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace JobBoard.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public Guid UId { get; set; }
}