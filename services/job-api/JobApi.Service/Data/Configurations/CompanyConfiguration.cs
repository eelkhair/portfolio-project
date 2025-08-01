using Elkhair.Dev.Common.Data;
using JobApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApi.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.ConfigureBaseAuditableEntity();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(250);
        builder.HasMany(c=>c.Jobs).WithOne(j=>j.Company).HasForeignKey(j=>j.CompanyId);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(j => j.About)
            .HasMaxLength(2000);

        builder.Property(j => j.EEO)
            .HasMaxLength(500);
    }
}