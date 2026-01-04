
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public sealed class ResponsibilityConfiguration : IEntityTypeConfiguration<Responsibility>
{
    public void Configure(EntityTypeBuilder<Responsibility> builder)
    {
        // Table
        builder.ToTable("Responsibilities", "Job");
        builder.ConfigureAuditableProperties();
        builder.ConfigureBusinessEntity();

        // Properties
        builder.Property(q => q.Value)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(q => q.JobId)
            .IsRequired();

        // Relationships
        builder.HasOne(q => q.Job)
            .WithMany(j => j.Responsibilities)
            .HasForeignKey(q => q.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(q => new { q.JobId, q.Value });
    }
}
