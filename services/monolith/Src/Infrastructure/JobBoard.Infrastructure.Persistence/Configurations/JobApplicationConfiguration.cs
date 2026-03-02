using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplications", "Application");

        builder.ConfigureBusinessEntity();
        builder.ConfigureAuditableProperties();

        builder.Property(a => a.CoverLetter)
            .HasMaxLength(5000);

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(a => a.Job)
            .WithMany()
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.Resume)
            .WithMany()
            .HasForeignKey(a => a.ResumeId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(a => a.JobId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.JobId, a.UserId }).IsUnique();
    }
}
