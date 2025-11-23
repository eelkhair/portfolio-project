using Elkhair.Dev.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApi.Infrastructure.Data.Entities;

namespace UserApi.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.ConfigureBaseAuditableEntity();
        builder.Property(u => u.Email).HasMaxLength(100);
        builder.Property(u => u.FirstName).HasMaxLength(50);
        builder.Property(u => u.LastName).HasMaxLength(50);
        builder.Property(u => u.Auth0UserId).HasMaxLength(50);
        builder.HasMany(u => u.UserCompanies).WithOne(uc => uc.User).HasForeignKey(uc => uc.UserId);
    }
}