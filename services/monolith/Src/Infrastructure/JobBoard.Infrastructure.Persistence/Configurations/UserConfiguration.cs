using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobBoard.Infrastructure.Persistence.Configurations;

public class UserCompanyConfiguration : IEntityTypeConfiguration<UserCompany>
{
    public void Configure(EntityTypeBuilder<UserCompany> builder)
    {
        builder.ToTable("UserCompanies", "User");

        builder.ConfigureAuditableProperties();
        builder.ConfigureBusinessEntity();

        builder.HasIndex(uc => new { uc.UserId, uc.CompanyId })
            .IsUnique();

        builder.HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
     
        builder.HasOne(uc => uc.Company)
            .WithMany()
            .HasForeignKey(uc => uc.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(uc => uc.UserId);
        builder.HasIndex(uc => uc.CompanyId);
    }
}