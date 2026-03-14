using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.ToTable("Drafts", "Draft");

        builder.ConfigureBusinessEntity();
        builder.ConfigureAuditableProperties();

        builder.Property(d => d.CompanyId)
            .IsRequired();

        builder.Property(d => d.DraftType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.DraftStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.ContentJson)
            .IsRequired();

        builder.HasIndex(d => d.CompanyId);
        builder.HasIndex(d => new { d.CompanyId, d.DraftStatus });
    }
}
