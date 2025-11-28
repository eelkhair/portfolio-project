using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations.Users;

public class UserConfiguration: IEntityTypeConfiguration<Domain.Entities.Users.User>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Users.User> builder)
    {
        builder.ToTable("Users", "User");
        
        builder.ConfigureAuditableProperties();
        builder.ConfigureBusinessEntity();
        
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
        builder.Property(c=> c.ExternalId)
            .HasMaxLength(100);
        builder.HasIndex(u => u.ExternalId).IsUnique();
        builder.HasIndex(u=> u.Email).IsUnique();
    }
}