using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations.Users;

public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("Resumes", "User");

        builder.ConfigureBusinessEntity();
        builder.ConfigureAuditableProperties();

        builder.Property(r => r.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.OriginalFileName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.ContentType)
            .HasMaxLength(100);

        builder.Property(r => r.FileSize);

        builder.Property(r => r.ParsedContent)
            .HasMaxLength(10000);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.UserId);
    }
}
