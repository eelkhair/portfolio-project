using CompanyApi.Infrastructure.Data.Entities;
using Elkhair.Dev.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyApi.Infrastructure.Data.Configurations;

public class IndustryConfiguration : IEntityTypeConfiguration<Industry>
{
    public void Configure(EntityTypeBuilder<Industry> builder)
    {
        builder.ToTable("Industries");
        builder.ConfigureBaseAuditableEntity();
        builder.Property(i => i.Name).IsRequired().HasMaxLength(250);
        builder.HasMany(i => i.Companies).WithOne(c => c.Industry).HasForeignKey(c => c.IndustryId);
    }
}