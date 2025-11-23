using Elkhair.Dev.Common.Data;
using JobApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApi.Infrastructure.Data.Configurations;

public class ResponsibilityConfiguration: IEntityTypeConfiguration<Responsibility>
{
    public void Configure(EntityTypeBuilder<Responsibility> builder)
    {
        builder.ToTable("Responsibilities");
        builder.ConfigureBaseAuditableEntity();
        builder.HasOne(c=> c.Job).WithMany(c=> c.Responsibilities).HasForeignKey(c=>c.JobId);
        builder.Property(c=> c.Value).HasMaxLength(250);
    }
}