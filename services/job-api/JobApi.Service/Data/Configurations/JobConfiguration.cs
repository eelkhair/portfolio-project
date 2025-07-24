using Elkhair.Dev.Common.Data;
using JobApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApi.Data.Configurations;

public class JobConfiguration: IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");
        builder.ConfigureBaseAuditableEntity();
        builder.HasOne(c=> c.Company).WithMany(j=>j.Jobs).HasForeignKey(j=>j.CompanyId);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(250);
        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(j => j.JobType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.AboutRole)
            .IsRequired()
            .HasMaxLength(3000);

        builder.Property(j => j.SalaryRange)
            .HasMaxLength(100);

        builder.Property(j => j.PostedAt)
            .HasMaxLength(100);

        builder.Property(j => j.AboutCompany)
            .HasMaxLength(2000);

        builder.Property(j => j.EEO)
            .HasMaxLength(500);
        

    }
}