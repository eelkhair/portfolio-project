using System.ComponentModel.DataAnnotations;

namespace Elkhair.Dev.Common.Domain.Entities;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    public Guid UId { get; set; }
}