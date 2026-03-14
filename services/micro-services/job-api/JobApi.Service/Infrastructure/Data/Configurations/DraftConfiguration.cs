using Elkhair.Dev.Common.Data;
using JobApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApi.Infrastructure.Data.Configurations;

public class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.ToTable("Drafts");
        builder.ConfigureBaseAuditableEntity();
        builder.HasOne(d => d.Company).WithMany().HasForeignKey(d => d.CompanyId);

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
