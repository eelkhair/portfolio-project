using CompanyApi.Infrastructure.Data.Entities;
using Elkhair.Dev.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyApi.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.ConfigureBaseAuditableEntity();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(250);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(c=> c.Description).HasMaxLength(4000);
        builder.Property(c=> c.Website).HasMaxLength(200);
        builder.Property(c => c.Logo).HasMaxLength(400);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(100).IsRequired();
        builder.Property(c=> c.Size).HasMaxLength(30);
        builder.Property(j => j.About)
            .HasMaxLength(2000);

        builder.Property(j => j.EEO)
            .HasMaxLength(500);
        
        builder.Property(c=> c.IndustryId).IsRequired();
        builder.Property(c=> c.Status).IsRequired().HasMaxLength(30);
        
        builder.HasOne(c => c.Industry).WithMany(i => i.Companies).HasForeignKey(c => c.IndustryId);
    }
}