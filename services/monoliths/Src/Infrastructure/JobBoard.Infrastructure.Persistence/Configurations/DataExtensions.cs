using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public static class DataExtensions
{
    private static void ConfigureBaseProperties<TBaseEntity>(this EntityTypeBuilder<TBaseEntity> builder, bool sequentialId = false)
        where TBaseEntity : BaseEntity
    {
        builder.HasKey(e => e.UId);
        if (sequentialId)
        { 
            builder.Property(e => e.UId).HasDefaultValueSql("newsequentialid()");
        }
        else
        {
            builder.Property(e => e.UId).IsRequired().ValueGeneratedNever();
        }
    }

    internal static void ConfigureBusinessEntity<TBusinessEntity>(this EntityTypeBuilder<TBusinessEntity> builder,
        bool isTemporal = true, bool isSequential = false) where TBusinessEntity : BaseEntity
    {
        builder.ConfigureBaseProperties(isSequential);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.HasIndex(e => e.Id).IsUnique();

        if (isTemporal)
        {
            builder.ToTable(x => x.IsTemporal());
        }
        
    }

    internal static void ConfigureInfrastructureEntity<TInfraEntity>(this EntityTypeBuilder<TInfraEntity> builder)
        where TInfraEntity : BaseEntity
    {
        builder.ConfigureBaseProperties(true);
        builder.Property(e => e.Id).UseIdentityColumn();
    }

    internal static void ConfigureAuditableProperties<TAuditableEntity>(
        this EntityTypeBuilder<TAuditableEntity> builder) where TAuditableEntity : BaseAuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2");
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

    }
}