using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs", "Job");
         
        builder.ConfigureBusinessEntity();
        builder.ConfigureAuditableProperties();

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(j => j.AboutRole)
            .IsRequired()
            .HasMaxLength(3000);

        builder.Property(j => j.SalaryRange)
            .HasMaxLength(100);

        builder.Property(j => j.JobType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(j => j.CompanyId)
            .IsRequired();
        
        builder.HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(j => j.Responsibilities)
            .WithOne(r => r.Job)
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.Qualifications)
            .WithOne(q => q.Job)
            .HasForeignKey(q => q.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        

        builder.Navigation(j => j.Responsibilities)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);

        builder.Navigation(j => j.Qualifications)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);
        
        builder.HasIndex(j => j.CompanyId);
        builder.HasIndex(j => new { j.Title, j.Location });
        builder.HasIndex(j => new { j.CompanyId, j.JobType });
    }
}
