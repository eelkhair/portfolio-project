using JobBoard.Domain.Entities;

namespace JobBoard.Domain.Aggregates;

public static class EntityFactory
{
    public static T ApplyAudit<T>(T entity, DateTime? createdAt, string? createdBy)
        where T : BaseAuditableEntity
    {
        if (createdAt.HasValue)
        {
            entity.CreatedAt = createdAt.Value;
            entity.UpdatedAt = createdAt.Value;
        }

        if (!string.IsNullOrEmpty(createdBy))
        {
            entity.CreatedBy = createdBy;
            entity.UpdatedBy = createdBy;
        }

        return entity;
    }
}