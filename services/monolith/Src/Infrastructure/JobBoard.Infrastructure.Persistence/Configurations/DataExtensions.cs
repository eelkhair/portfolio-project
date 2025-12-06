using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public static class DataExtensions
{
    public static void ConfigureBusinessEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        var table = builder.Metadata.GetTableName()!;
        var schema = builder.Metadata.GetSchema() ?? "dbo";
        var sequence = $"{table}_Sequence";

        builder.HasKey("InternalId");

        builder.Property<int>("InternalId")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql($"NEXT VALUE FOR {sequence}");

        builder.Property<Guid>("Id")
            .IsRequired()
            .ValueGeneratedNever();
        
        builder.ToTable(table, schema, tb =>
        {
            tb.IsTemporal();
        });
    }
    
    public static void ConfigureAuditableProperties<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTime>("CreatedAt")
            .IsRequired();

        builder.Property<string>("CreatedBy")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property<DateTime>("UpdatedAt")
            .IsRequired();

        builder.Property<string>("UpdatedBy")
            .HasMaxLength(100)
            .IsRequired();
    }


    public static void ConfigureInfrastructureEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.HasKey("InternalId");

        builder.Property<int>("InternalId")
            .UseIdentityColumn();

        builder.Property<Guid>("Id")
            .IsRequired()
            .HasDefaultValueSql("newsequentialid()");
    }
}
