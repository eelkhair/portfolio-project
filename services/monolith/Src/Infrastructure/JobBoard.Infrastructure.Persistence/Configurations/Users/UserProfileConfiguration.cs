using JobBoard.Domain.Entities.Users;
using JobBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations.Users;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles", "User");

        builder.ConfigureBusinessEntity();
        builder.ConfigureAuditableProperties();

        builder.Property(p => p.Phone)
            .HasMaxLength(50);

        builder.Property(p => p.LinkedIn)
            .HasMaxLength(300);

        builder.Property(p => p.Portfolio)
            .HasMaxLength(300);

        builder.Property(p => p.Skills)
            .HasMaxLength(1000);

        builder.Property(p => p.PreferredLocation)
            .HasMaxLength(150);

        builder.Property(p => p.PreferredJobType)
            .HasConversion<int?>();

        builder.OwnsMany(p => p.WorkHistory, wh =>
        {
            wh.ToJson();
        });

        builder.OwnsMany(p => p.Education, ed =>
        {
            ed.ToJson();
        });

        builder.OwnsMany(p => p.Certifications, cert =>
        {
            cert.ToJson();
        });

        builder.HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
