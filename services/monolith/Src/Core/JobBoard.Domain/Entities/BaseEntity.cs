// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace JobBoard.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public Guid UId { get; set; }
}