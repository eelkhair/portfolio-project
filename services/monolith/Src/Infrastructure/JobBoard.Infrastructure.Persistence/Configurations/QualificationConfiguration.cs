
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public sealed class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.ToTable("Qualifications", "Job");
        builder.ConfigureAuditableProperties();
        builder.ConfigureBusinessEntity();
        
        builder.Property(q => q.Value)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(q => q.JobId)
            .IsRequired();
        
        builder.HasOne(q => q.Job)
            .WithMany(j => j.Qualifications)
            .HasForeignKey(q => q.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(q => new { q.JobId, q.Value });
    }
}
