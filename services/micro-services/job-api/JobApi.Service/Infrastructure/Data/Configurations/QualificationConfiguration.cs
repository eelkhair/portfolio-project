using Elkhair.Dev.Common.Data;
using JobApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApi.Infrastructure.Data.Configurations;

public class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.ToTable("Qualifications");
        builder.ConfigureBaseAuditableEntity();
        builder.HasOne(c=> c.Job).WithMany(c=> c.Qualifications).HasForeignKey(c=>c.JobId);
        builder.Property(c=> c.Value).HasMaxLength(250);
    }
}